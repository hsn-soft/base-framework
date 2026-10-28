using System.Linq.Expressions;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Services;
using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Exceptions;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.InvoiceService.Application.Services;

/// <summary>
/// The use cases of the invoices, and the example of every kind of permission. Endpoint permissions are asked by the controller (the attribute of each action). Operations are asked here, by the code that does the thing: what the caller may see of an invoice. Constraints
/// are asked here too, first of all (a caller without one is refused before the store is touched): how much an invoice may amount to for the caller to see it and to approve it.
/// </summary>
public sealed class InvoiceAppService(
    IInvoiceRepository invoices,
    IBlobStore blobs,
    IEventBus eventBus,
    IPermissionChecker permissions,
    TimeProvider time,
    IOptions<InvoiceStorageOptions> storage,
    IServiceProvider services) : ApplicationService(services), IInvoiceAppService
{
    public async Task<InvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // constraint: a caller without a view limit is refused before the store is asked; an invoice above the limit is not found
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        Invoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return invoice.Total is null || invoice.Total <= limit ? ToDto(invoice, await SightAsync(cancellationToken).ConfigureAwait(false)) : throw InvoiceNotFound.For(id);
    }

    public async Task<PagedDataResultDto<InvoiceDto>> GetPagedListAsync(GetInvoicesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        // constraint: the list holds the invoices up to the limit of the caller (those with no total yet too); a caller without one is refused before the store is asked
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PagedQueryResult<Invoice> page = await invoices.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput, limit), InvoiceSorting.Fields, InvoiceSorting.Default), cancellationToken).ConfigureAwait(false);
        Sight sight = await SightAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, invoice => ToDto(invoice, sight));
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxApproveAmount, cancellationToken).ConfigureAwait(false);
        Invoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (invoice.Total > limit)
        {
            throw new InvoiceOverLimitException(invoice.Total.Value, limit);
        }

        invoice.Approve(time.GetUtcNow(), CurrentUser.Id);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

        // the signing is not part of the request: the service tells itself, and the event carries who approved
        await eventBus.PublishAsync(new InvoiceApprovedEto(invoice.Id, invoice.QueueId, invoice.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was approved", invoice.InvoiceId, invoice.Number);
    }

    public async Task RejectAsync(Guid id, InvoiceRejectDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Invoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        invoice.Reject(input.Reason, time.GetUtcNow(), CurrentUser.Id);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new InvoiceRejectedEto(invoice.QueueId, invoice.InvoiceId, invoice.RejectionReason!), cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was rejected", invoice.InvoiceId, invoice.Number);
    }

    public async Task<InvoiceXmlDto> GetXmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // the XML has the tax ids and the prices in it: only a caller who may see both is given it
        foreach (string operation in new[] { EdxOperationPermissions.Invoice.TaxIdView, EdxOperationPermissions.Invoice.PriceView })
        {
            if (!await permissions.IsGrantedAsync(operation, cancellationToken).ConfigureAwait(false))
            {
                throw new BaseHttpException(StatusCodes.Status403Forbidden, $"The permission '{operation}' is required to read the XML of an invoice.").WithKey("operation_not_granted", operation);
            }
        }

        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        Invoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        string? path = invoice.SignedPath ?? invoice.DraftPath;
        if (path is null || invoice.Total > limit)
        {
            throw InvoiceNotFound.For(id);
        }

        Stream stream = await blobs.OpenReadAsync(storage.Value.Container, path, cancellationToken).ConfigureAwait(false) ?? throw InvoiceNotFound.For(id);
        await using (stream.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            return new InvoiceXmlDto { FileName = $"{(invoice.SignedPath is null ? "draft" : "signed")}_{invoice.InvoiceId:N}.xml", Content = copy.ToArray() };
        }
    }

    // what the caller may see: asked once for a list, not for every invoice of it
    private async Task<Sight> SightAsync(CancellationToken cancellationToken)
        => new(
            await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.TaxIdView, cancellationToken).ConfigureAwait(false),
            await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.PriceView, cancellationToken).ConfigureAwait(false));

    private async Task<Invoice> FindAsync(Guid id, CancellationToken cancellationToken)
        => await invoices.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw InvoiceNotFound.For(id);

    private static Expression<Func<Invoice, bool>> Filter(GetInvoicesPaged input, decimal limit)
    {
        string? number = Normalized(input.Number);
        string? party = string.IsNullOrWhiteSpace(input.PartyName) ? null : input.PartyName.Trim();
        Guid? invoiceId = input.InvoiceId;
        InvoiceStatus? status = string.IsNullOrWhiteSpace(input.Status)
            ? null
            : Enum.TryParse(input.Status.Trim(), ignoreCase: true, out InvoiceStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("filter_value_invalid", $"'{input.Status}' is not a status of an invoice.").WithKey("filter_value_invalid", nameof(input.Status), input.Status);
        return new FilterBuilder<Invoice>()
            .And(invoice => invoice.Total == null || invoice.Total <= limit)
            .And(invoiceId is null ? null : invoice => invoice.InvoiceId == invoiceId.Value)
            .And(number is null ? null : invoice => invoice.NormalizedNumber.Contains(number))
            .And(party is null ? null : invoice => invoice.SupplierName!.Contains(party) || invoice.CustomerName!.Contains(party))
            .And(status is null ? null : invoice => invoice.Status == status.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static InvoiceDto ToDto(Invoice invoice, Sight sight) => new()
    {
        Id = invoice.Id,
        InvoiceId = invoice.InvoiceId,
        QueueId = invoice.QueueId,
        Number = invoice.Number,
        SourceType = invoice.SourceType.ToString(),
        ChannelType = invoice.ChannelType.ToString(),
        Status = invoice.Status.ToString(),
        IsLive = invoice.IsLive,
        FailureStage = invoice.FailureStage,
        FailureReason = invoice.FailureReason,
        StatusChangedAt = invoice.StatusChangedAt,
        IssueDate = invoice.IssueDate,
        Currency = invoice.Currency,
        SupplierName = invoice.SupplierName,
        SupplierTaxId = sight.TaxIds ? invoice.SupplierTaxId : null,
        CustomerName = invoice.CustomerName,
        CustomerTaxId = sight.TaxIds ? invoice.CustomerTaxId : null,
        Lines = [.. invoice.Lines.Select(line => new InvoiceLineDto
        {
            Description = line.Description,
            Quantity = line.Quantity,
            UnitPrice = sight.Amounts ? line.UnitPrice : null,
            VatRate = line.VatRate,
            Total = sight.Amounts ? line.Total : null,
        })],
        Total = sight.Amounts ? invoice.Total : null,
        ApprovedAt = invoice.ApprovedAt,
        ApprovedBy = invoice.ApprovedBy,
        RejectedAt = invoice.RejectedAt,
        RejectedBy = invoice.RejectedBy,
        RejectionReason = invoice.RejectionReason,
        SignedAt = invoice.SignedAt,
        TenantId = invoice.TenantId,
        CreatedAt = invoice.CreatedAt,
        CreatedBy = invoice.CreatedBy,
        UpdatedAt = invoice.UpdatedAt,
        UpdatedBy = invoice.UpdatedBy,
    };

    private readonly record struct Sight(bool TaxIds, bool Amounts);
}
