using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml;
using Edx.IntakeService.Application.Archives;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Domain.StorageDomain.Entities;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Domain.StoredFileDomain.Exceptions;
using Edx.IntakeService.Domain.StoredFileDomain.Repositories;
using Edx.Shared.Contracts.Events.Intake;
using Edx.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.IntakeService.Application.Services;

/// <summary>
/// The use cases of the service. A file is received in this order: the folders of the hierarchy are made (a record, and an event, for each new one), the bytes are written to the blob store at the object name the layout gives, the
/// record of the file is written, and the event of the record is published. There is no transaction over the store, the database and the broker: when the record cannot be written the bytes are taken out again, and when the event cannot be
/// published the caller is told and the file stays recorded (an outbox would close that gap; the system does not have one yet).
/// </summary>
public sealed class IntakeAppService(
    IRepository<StoredFile, Guid> files,
    StorageLayout layout,
    ArchiveInspector inspector,
    IBlobStore blobs,
    IEventBus eventBus,
    IPermissionChecker permissions,
    TimeProvider time,
    IOptions<StorageOptions> storage,
    IOptions<IntakeOptions> intake,
    IServiceProvider services) : ApplicationService(services), IIntakeAppService
{
    private const string ZipContentType = "application/zip";
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public Task<StoredFileDto> ReceiveXmlArchiveAsync(string fileName, Stream content, IntakeChannel channel, CancellationToken cancellationToken = default)
        => ReceiveArchiveAsync(FileKind.XmlArchive, ArchiveKind.Xml, fileName, content, channel, cancellationToken);

    public Task<StoredFileDto> ReceiveTabularArchiveAsync(string fileName, Stream content, IntakeChannel channel, CancellationToken cancellationToken = default)
        => ReceiveArchiveAsync(FileKind.TabularArchive, ArchiveKind.Tabular, fileName, content, channel, cancellationToken);

    public async Task<StoredFileDto> ReceiveInvoiceModelAsync(InvoiceSubmissionDto model, ModelFormat format, IntakeChannel channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // the number of the invoice names the file, so a customer finds it by the number it knows
        string fileName = $"{FileNaming.Clean(model.InvoiceNumber)}.{(format == ModelFormat.Xml ? "xml" : "json")}";
        using MemoryStream body = new(format == ModelFormat.Xml ? ToXml(model) : ToJson(model));
        return await StoreAsync(
            format == ModelFormat.Xml ? FileKind.ModelXml : FileKind.ModelJson,
            channel,
            fileName,
            format == ModelFormat.Xml ? "application/xml" : "application/json",
            body,
            [],
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<StoredFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false), await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<StoredFileDto>> GetPagedListAsync(GetStoredFilesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<StoredFile> page = await files.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput), StoredFileSorting.Fields, StoredFileSorting.Default), cancellationToken).ConfigureAwait(false);
        bool location = await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, file => ToDto(file, location));
    }

    public async Task<StoredFileContent> OpenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StoredFile file = await FindAsync(id, cancellationToken).ConfigureAwait(false);

        // the container and the object name are the ones the file was written with, not the ones of today's settings: a file stays readable after the settings move
        Stream content = await blobs.OpenReadAsync(file.Container, file.ObjectName, cancellationToken).ConfigureAwait(false)
                         ?? throw StoredFileNotFound.For(id);
        return new StoredFileContent(content, file.FileName, file.ContentType);
    }

    private async Task<StoredFileDto> ReceiveArchiveAsync(FileKind kind, ArchiveKind archiveKind, string fileName, Stream content, IntakeChannel channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        // the archive is read twice (looked into, then stored) and a zip is read from its end: it is kept in a temporary file that goes away when it is closed
        FileStream spool = new(Path.Combine(Path.GetTempPath(), $"edx-intake-{Guid.NewGuid():N}.tmp"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        await using (spool.ConfigureAwait(false))
        {
            await CopyLimitedAsync(content, spool, await UploadLimitAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ArchiveEntry> entries = inspector.Inspect(spool, archiveKind);
            return await StoreAsync(kind, channel, fileName, ZipContentType, spool, entries, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<StoredFileDto> StoreAsync(FileKind kind, IntakeChannel channel, string fileName, string contentType, Stream content, IReadOnlyList<ArchiveEntry> entries, CancellationToken cancellationToken)
    {
        DateTimeOffset receivedAt = time.GetUtcNow();
        StorageFolder folder = await layout.EnsureFolderAsync(receivedAt, cancellationToken).ConfigureAwait(false);

        Guid id = Guid.CreateVersion7();
        string cleanName = FileNaming.Clean(fileName);
        string container = storage.Value.Container;
        string objectName = layout.ObjectName(folder.Path, FileNaming.ObjectFileName(id, cleanName));

        content.Position = 0;
        long size = content.Length;
        await blobs.SaveAsync(container, objectName, content, contentType, cancellationToken).ConfigureAwait(false);

        StoredFile file = StoredFile.Create(id, kind, channel, cleanName, contentType, size, new StorageFolderRef(folder.Id, folder.Path), container, objectName, entries);
        try
        {
            await files.AddAsync(file, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // a file nobody has a record of is a file nobody can find: it is taken out again (the failure itself goes on)
            await blobs.DeleteAsync(container, objectName, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        await eventBus.PublishAsync(
            new FileStoredEto(file.Id, kind.ToString(), channel.ToString(), file.FileName, size, contentType, container, objectName, folder.Path, Math.Max(entries.Count, 1), receivedAt),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("File {StoredFileId} ({FileKind}, {FileChannel}, {FileSizeBytes} bytes) was stored in {FolderPath}", file.Id, kind, channel, size, folder.Path);
        return ToDto(file, await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false));
    }

    // the most the caller may send: what the service allows, or less when the caller's role (or the caller) was given a limit (constraint.intake.upload.max-bytes)
    private async Task<long> UploadLimitAsync(CancellationToken cancellationToken)
        => Math.Min(intake.Value.MaxUploadBytes, await permissions.GetLongAsync(EdxConstraintPermissions.Intake.UploadMaxBytes, cancellationToken).ConfigureAwait(false) ?? long.MaxValue);

    // where a file is kept (the container, the object name, the folder) is shown to who was granted the operation, and empty to everyone else
    private Task<bool> CanSeeLocationAsync(CancellationToken cancellationToken) => permissions.IsGrantedAsync(EdxOperationPermissions.Intake.FileStorageLocationView, cancellationToken);

    private static async Task CopyLimitedAsync(Stream source, Stream target, long limit, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > limit)
            {
                throw new InvalidArchiveException(InvalidArchiveException.TooLarge, $"A file may weigh at most {limit} bytes.", limit);
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static byte[] ToJson(InvoiceSubmissionDto model) => JsonSerializer.SerializeToUtf8Bytes(model, s_json);

    // the same XML a WCF client puts on the wire (the data contract of the model)
    private static byte[] ToXml(InvoiceSubmissionDto model)
    {
        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
        {
            new DataContractSerializer(typeof(InvoiceSubmissionDto)).WriteObject(writer, model);
        }

        return stream.ToArray();
    }

    private async Task<StoredFile> FindAsync(Guid id, CancellationToken cancellationToken)
        => await files.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw StoredFileNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<StoredFile, bool>> Filter(GetStoredFilesPaged input)
    {
        string? name = string.IsNullOrWhiteSpace(input.FileName) ? null : input.FileName.Trim();
        FileKind? kind = Parse<FileKind>(input.Kind, nameof(input.Kind));
        IntakeChannel? channel = Parse<IntakeChannel>(input.Channel, nameof(input.Channel));
        string? search = string.IsNullOrWhiteSpace(input.SearchText) ? null : input.SearchText.Trim();
        return new FilterBuilder<StoredFile>()
            .And(name is null ? null : file => file.FileName.Contains(name))
            .And(search is null ? null : file => file.FileName.Contains(search) || file.FolderPath.Contains(search))
            .And(kind is null ? null : file => file.Kind == kind.Value)
            .And(channel is null ? null : file => file.Channel == channel.Value)
            .Build();
    }

    private static T? Parse<T>(string? text, string field)
        where T : struct, Enum
        => string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.TryParse(text.Trim(), ignoreCase: true, out T value) && Enum.IsDefined(value)
                ? value
                : throw new BusinessException("filter_value_invalid", $"'{text}' is not a value of {field}.").WithKey("filter_value_invalid", field, text);

    private static StoredFileDto ToDto(StoredFile file, bool showLocation) => new()
    {
        Id = file.Id,
        Kind = file.Kind.ToString(),
        Channel = file.Channel.ToString(),
        FileName = file.FileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        FolderPath = showLocation ? file.FolderPath : string.Empty,
        Container = showLocation ? file.Container : string.Empty,
        ObjectName = showLocation ? file.ObjectName : string.Empty,
        Entries = [.. file.Entries.Select(entry => new StoredFileEntryDto { Name = entry.Name, SizeBytes = entry.SizeBytes })],
        TenantId = file.TenantId,
        CreatedAt = file.CreatedAt,
        CreatedBy = file.CreatedBy,
    };
}
