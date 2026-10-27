using System.Linq.Expressions;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Services;
using Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;
using Edx.InvoiceService.Domain.ReceivedFileDomain.Exceptions;
using Edx.InvoiceService.Domain.ReceivedFileDomain.Repositories;
using Edx.Shared.Contracts.Events.Intake;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using Microsoft.Extensions.Logging;

namespace Edx.InvoiceService.Application.Services;

public sealed class ReceivedFileAppService(IRepository<ReceivedFile, Guid> files, IServiceProvider services) : ApplicationService(services), IReceivedFileAppService
{
    public async Task RecordAsync(FileStoredEto received, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(received);

        // the inbox keeps a message from being handled twice; this keeps the same file from being recorded twice even when it was announced twice (a message put back from the error queue is another message)
        if (await files.AnyAsync(file => file.FileId == received.FileId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            Logger.LogInformation("File {StoredFileId} is already recorded", received.FileId);
            return;
        }

        await files.AddAsync(
            ReceivedFile.Record(
                received.FileId,
                received.Kind,
                received.Channel,
                received.FileName,
                received.SizeBytes,
                received.ContentType,
                received.Container,
                received.ObjectName,
                received.FolderPath,
                received.EntryCount,
                received.ReceivedAt),
            cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("File {StoredFileId} ({FileKind}, {FileChannel}) was recorded", received.FileId, received.Kind, received.Channel);
    }

    public async Task<ReceivedFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await files.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ReceivedFileNotFound.For(id));

    public async Task<PagedDataResultDto<ReceivedFileDto>> GetPagedListAsync(GetReceivedFilesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<ReceivedFile> page = await files.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput), ReceivedFileSorting.Fields, ReceivedFileSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    private static Expression<Func<ReceivedFile, bool>> Filter(GetReceivedFilesPaged input)
    {
        string? name = string.IsNullOrWhiteSpace(input.FileName) ? null : input.FileName.Trim();
        string? kind = string.IsNullOrWhiteSpace(input.Kind) ? null : input.Kind.Trim();
        string? search = string.IsNullOrWhiteSpace(input.SearchText) ? null : input.SearchText.Trim();
        return new FilterBuilder<ReceivedFile>()
            .And(name is null ? null : file => file.FileName.Contains(name))
            .And(kind is null ? null : file => file.Kind == kind)
            .And(search is null ? null : file => file.FileName.Contains(search) || file.FolderPath.Contains(search))
            .Build();
    }

    private static ReceivedFileDto ToDto(ReceivedFile file) => new()
    {
        Id = file.Id,
        FileId = file.FileId,
        Kind = file.Kind,
        Channel = file.Channel,
        FileName = file.FileName,
        SizeBytes = file.SizeBytes,
        ContentType = file.ContentType,
        Container = file.Container,
        ObjectName = file.ObjectName,
        FolderPath = file.FolderPath,
        EntryCount = file.EntryCount,
        ReceivedAt = file.ReceivedAt,
        TenantId = file.TenantId,
        CreatedAt = file.CreatedAt,
        CreatedBy = file.CreatedBy,
    };
}
