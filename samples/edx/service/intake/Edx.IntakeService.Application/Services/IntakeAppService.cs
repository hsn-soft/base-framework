using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Application.Uploads;
using Edx.IntakeService.Domain.QueueDomain.Entities;
using Edx.IntakeService.Domain.QueueDomain.Exceptions;
using Edx.IntakeService.Domain.QueueDomain.Repositories;
using Edx.IntakeService.Domain.StorageDomain.Entities;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Contracts.Invoicing;
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
/// The use cases of the service. An invoice is queued in this order: it is checked (the fields, the hash, the zip), the folders of the hierarchy are made (a record, and an event, for each new one), the bytes are written to the blob store at the object name the layout gives, the
/// queue item is written and the event of the queue item is published. There is no transaction over the store, the database and the broker: when the queue item cannot be written the bytes are taken out again, and when the event cannot be published the caller is told and the
/// item stays in the queue (an outbox would close that gap; the system does not have one yet).
/// </summary>
public sealed class IntakeAppService(
    IRepository<QueueItem, Guid> queue,
    StorageLayout layout,
    UploadInspector inspector,
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

    public async Task<QueueItemDto> QueueFileAsync(InvoiceUploadDto input, Stream content, InvoiceChannelType channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(content);

        // what the sender has to say of the zip: all of it, and a type a zip can hold
        if (input.InvoiceId is not { } invoiceId || invoiceId == Guid.Empty || string.IsNullOrWhiteSpace(input.InvoiceNumber) || string.IsNullOrWhiteSpace(input.FileHash) || input.SourceType is not { } sourceType || sourceType == InvoiceSourceType.Model)
        {
            throw new InvalidUploadException(InvalidUploadException.FieldMissing, "The invoice id, the invoice number, the file hash and the source type (xml, csv, txt or xls) are required.");
        }

        // the zip is read twice (hashed, looked into) and a zip is read from its end: it is kept in a temporary file that goes away when it is closed
        FileStream spool = new(Path.Combine(Path.GetTempPath(), $"edx-intake-{Guid.NewGuid():N}.tmp"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
        await using (spool.ConfigureAwait(false))
        {
            await CopyLimitedAsync(content, spool, await UploadLimitAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            spool.Position = 0;
            string hash = Convert.ToHexString(await SHA256.HashDataAsync(spool, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
            if (!hash.Equals(input.FileHash.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidUploadException(InvalidUploadException.HashMismatch, "The hash of the zip is not the one the sender gave: the file was changed on the way, or the hash is of something else (it is the SHA-256 of the zip).", hash);
            }

            UploadInspection inspection = inspector.Inspect(spool, sourceType);
            return await StoreAsync(invoiceId, input.InvoiceNumber, sourceType, channel, hash, FileNaming.Clean(input.FileName), ZipContentType, spool, inspection.EntryName, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<QueueItemDto> QueueModelAsync(InvoiceSubmissionDto model, ModelFormat format, InvoiceChannelType channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // a model that comes through gRPC or WCF is built by the entry point, so it is checked here, once, for all of them
        List<ValidationResult> problems = [];
        if (!Validator.TryValidateObject(model, new ValidationContext(model), problems, validateAllProperties: true))
        {
            throw new BusinessException("model_validation_failed", string.Join(" ", problems.Select(problem => problem.ErrorMessage))).WithKey("model_validation_failed");
        }

        byte[] bytes = format == ModelFormat.Xml ? InvoiceXml.Write(model) : JsonSerializer.SerializeToUtf8Bytes(model, s_json);
        using MemoryStream body = new(bytes);
        string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string fileName = $"{FileNaming.Clean(model.InvoiceNumber)}.{(format == ModelFormat.Xml ? "xml" : "json")}";
        return await StoreAsync(model.InvoiceId, model.InvoiceNumber, InvoiceSourceType.Model, channel, hash, fileName, format == ModelFormat.Xml ? "application/xml" : "application/json", body, null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QueueItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false), await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false));

    public async Task<List<QueueItemDto>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QueueItem> found = await queue.GetListAsync(
            new ListQueryOptions<QueueItem> { Filter = item => item.InvoiceId == invoiceId, Sort = QueueItemSorting.Fields.Parse(null, QueueItemSorting.Default) }, cancellationToken).ConfigureAwait(false);
        if (found.Count == 0)
        {
            throw QueueItemNotFound.ForInvoice(invoiceId);
        }

        // an invoice that got through is answered with the item that got through; the ones that failed before it are history
        IReadOnlyList<QueueItem> answer = found.Any(item => item.IsSuccessful) ? [.. found.Where(item => item.IsSuccessful)] : found;
        bool location = await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false);
        return [.. answer.Select(item => ToDto(item, location))];
    }

    public async Task<PagedDataResultDto<QueueItemDto>> GetPagedListAsync(GetQueueItemsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<QueueItem> page = await queue.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput), QueueItemSorting.Fields, QueueItemSorting.Default), cancellationToken).ConfigureAwait(false);
        bool location = await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, item => ToDto(item, location));
    }

    public async Task<StoredFileContent> OpenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        QueueItem item = await FindAsync(id, cancellationToken).ConfigureAwait(false);

        // the container and the object name are the ones the file was written with, not the ones of today's settings: a file stays readable after the settings move
        Stream content = await blobs.OpenReadAsync(item.Container, item.ObjectName, cancellationToken).ConfigureAwait(false) ?? throw QueueItemNotFound.For(id);
        return new StoredFileContent(content, item.FileName, item.ContentType);
    }

    private async Task<QueueItemDto> StoreAsync(
        Guid invoiceId,
        string invoiceNumber,
        InvoiceSourceType sourceType,
        InvoiceChannelType channel,
        string hash,
        string fileName,
        string contentType,
        Stream content,
        string? entryName,
        CancellationToken cancellationToken)
    {
        DateTimeOffset receivedAt = time.GetUtcNow();
        StorageFolder folder = await layout.EnsureFolderAsync(receivedAt, cancellationToken).ConfigureAwait(false);

        Guid id = Guid.CreateVersion7();
        string container = storage.Value.Container;
        string objectName = layout.ObjectName(folder.Path, FileNaming.ObjectFileName(id, fileName));

        content.Position = 0;
        long size = content.Length;
        await blobs.SaveAsync(container, objectName, content, contentType, cancellationToken).ConfigureAwait(false);

        QueueItem item = QueueItem.Create(id, invoiceId, invoiceNumber.Trim(), intake.Value.ServiceName, sourceType, channel, hash, fileName, contentType, size, entryName, folder.Id, folder.Path, container, objectName, receivedAt);
        try
        {
            await queue.AddAsync(item, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // a file nobody has a record of is a file nobody can find: it is taken out again (the failure itself goes on)
            await blobs.DeleteAsync(container, objectName, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        await eventBus.PublishAsync(
            new InvoiceQueuedEto(item.Id, invoiceId, item.InvoiceNumber, item.ReceivedBy, sourceType, channel, hash, fileName, contentType, container, objectName, sourceType != InvoiceSourceType.Model, receivedAt),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}, {SourceType}, {ChannelType}, {SizeBytes} bytes) was queued as {QueueItemId}", invoiceId, item.InvoiceNumber, sourceType, channel, size, item.Id);
        return ToDto(item, await CanSeeLocationAsync(cancellationToken).ConfigureAwait(false));
    }

    // the most the caller may send: what the service allows, or less when the limit the caller's role was given is lower (constraint.intake.upload.max-bytes); a caller with no limit may send nothing (403)
    private async Task<long> UploadLimitAsync(CancellationToken cancellationToken)
        => Math.Min(intake.Value.MaxUploadBytes, await permissions.RequireLongAsync(EdxConstraintPermissions.Intake.UploadMaxBytes, cancellationToken).ConfigureAwait(false));

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
                throw new InvalidUploadException(InvalidUploadException.TooLarge, $"A file may weigh at most {limit} bytes.", limit);
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<QueueItem> FindAsync(Guid id, CancellationToken cancellationToken)
        => await queue.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw QueueItemNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<QueueItem, bool>> Filter(GetQueueItemsPaged input)
    {
        string? number = string.IsNullOrWhiteSpace(input.InvoiceNumber) ? null : input.InvoiceNumber.Trim();
        string? search = string.IsNullOrWhiteSpace(input.SearchText) ? null : input.SearchText.Trim();
        QueueStatus? status = Parse<QueueStatus>(input.Status, nameof(input.Status));
        InvoiceSourceType? source = Parse<InvoiceSourceType>(input.SourceType, nameof(input.SourceType));
        InvoiceChannelType? channel = Parse<InvoiceChannelType>(input.ChannelType, nameof(input.ChannelType));
        return new FilterBuilder<QueueItem>()
            .And(number is null ? null : item => item.InvoiceNumber.Contains(number))
            .And(search is null ? null : item => item.InvoiceNumber.Contains(search) || item.FileName.Contains(search))
            .And(status is null ? null : item => item.Status == status.Value)
            .And(source is null ? null : item => item.SourceType == source.Value)
            .And(channel is null ? null : item => item.ChannelType == channel.Value)
            .Build();
    }

    private static T? Parse<T>(string? text, string field)
        where T : struct, Enum
        => string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.TryParse(text.Trim(), ignoreCase: true, out T value) && Enum.IsDefined(value)
                ? value
                : throw new BusinessException("filter_value_invalid", $"'{text}' is not a value of {field}.").WithKey("filter_value_invalid", field, text);

    private static QueueItemDto ToDto(QueueItem item, bool showLocation) => new()
    {
        Id = item.Id,
        InvoiceId = item.InvoiceId,
        InvoiceNumber = item.InvoiceNumber,
        SourceType = item.SourceType.ToString(),
        ReceivedBy = item.ReceivedBy,
        ChannelType = item.ChannelType.ToString(),
        Status = item.Status.ToString(),
        StatusReason = item.StatusReason,
        StatusChangedAt = item.StatusChangedAt,
        FileName = item.FileName,
        FileHash = item.FileHash,
        ContentType = item.ContentType,
        SizeBytes = item.SizeBytes,
        EntryName = item.EntryName,
        FolderPath = showLocation ? item.FolderPath : string.Empty,
        Container = showLocation ? item.Container : string.Empty,
        ObjectName = showLocation ? item.ObjectName : string.Empty,
        TenantId = item.TenantId,
        CreatedAt = item.CreatedAt,
        CreatedBy = item.CreatedBy,
        UpdatedAt = item.UpdatedAt,
        UpdatedBy = item.UpdatedBy,
    };
}
