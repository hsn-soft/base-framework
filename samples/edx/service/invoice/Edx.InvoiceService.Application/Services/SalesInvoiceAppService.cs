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
public sealed class SalesInvoiceAppService(
    ISalesInvoiceRepository invoices,
    IBlobStore blobs,
    IEventBus eventBus,
    IPermissionChecker permissions,
    TimeProvider time,
    IOptions<InvoiceStorageOptions> storage,
    IServiceProvider services) : ApplicationService(services), ISalesInvoiceAppService
{
    public async Task<SalesInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // constraint: a caller without a view limit is refused before the store is asked; an invoice above the limit is not found
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        SalesInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return invoice.Total is null || invoice.Total <= limit ? ToDto(invoice, await SightAsync(cancellationToken).ConfigureAwait(false)) : throw InvoiceNotFound.For(id);
    }

    public async Task<PagedDataResultDto<SalesInvoiceDto>> GetPagedListAsync(GetSalesInvoicesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        // constraint: the list holds the invoices up to the limit of the caller (those with no total yet too); a caller without one is refused before the store is asked
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PagedQueryResult<SalesInvoice> page = await invoices.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput, limit), SalesInvoiceSorting.Fields, SalesInvoiceSorting.Default), cancellationToken).ConfigureAwait(false);
        Sight sight = await SightAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, invoice => ToDto(invoice, sight));
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxApproveAmount, cancellationToken).ConfigureAwait(false);
        SalesInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (invoice.Total > limit)
        {
            throw new InvoiceOverLimitException(invoice.Total.Value, limit);
        }

        invoice.Approve(time.GetUtcNow(), CurrentUser.Id);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

        // the signing is not part of the request and not of this service: the signer service handles the event (the event carries who approved)
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForTenant(invoice.TenantId) ?? throw new InvalidOperationException($"The tenant {invoice.TenantId} has no e-invoice account.");
        await eventBus.PublishAsync(
            new InvoiceApprovedEto(invoice.Id, invoice.QueueId, invoice.InvoiceId, invoice.Number, invoice.CustomerTaxId!, invoice.CustomerName!, profile.Title, storage.Value.Container, invoice.DraftPath!, SignedPathOf(invoice)),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was approved", invoice.InvoiceId, invoice.Number);
    }

    public async Task RejectAsync(Guid id, InvoiceRejectDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        SalesInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
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
        SalesInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
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

    private async Task<SalesInvoice> FindAsync(Guid id, CancellationToken cancellationToken)
        => await invoices.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw InvoiceNotFound.For(id);

    private static Expression<Func<SalesInvoice, bool>> Filter(GetSalesInvoicesPaged input, decimal limit)
    {
        string? number = Normalized(input.Number);
        string? party = string.IsNullOrWhiteSpace(input.PartyName) ? null : input.PartyName.Trim();
        Guid? invoiceId = input.InvoiceId;
        SalesInvoiceStatus? status = string.IsNullOrWhiteSpace(input.Status)
            ? null
            : Enum.TryParse(input.Status.Trim(), ignoreCase: true, out SalesInvoiceStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("filter_value_invalid", $"'{input.Status}' is not a status of an invoice.").WithKey("filter_value_invalid", nameof(input.Status), input.Status);
        InvoiceEnvelopeState? envelopeState = string.IsNullOrWhiteSpace(input.EnvelopeState)
            ? null
            : Enum.TryParse(input.EnvelopeState.Trim(), ignoreCase: true, out InvoiceEnvelopeState parsedState) && Enum.IsDefined(parsedState)
                ? parsedState
                : throw new BusinessException("filter_value_invalid", $"'{input.EnvelopeState}' is not an envelope state.").WithKey("filter_value_invalid", nameof(input.EnvelopeState), input.EnvelopeState);
        return new FilterBuilder<SalesInvoice>()
            .And(invoice => invoice.Total == null || invoice.Total <= limit)
            .And(invoiceId is null ? null : invoice => invoice.InvoiceId == invoiceId.Value)
            .And(number is null ? null : invoice => invoice.NormalizedNumber.Contains(number))
            .And(party is null ? null : invoice => invoice.SupplierName!.Contains(party) || invoice.CustomerName!.Contains(party))
            .And(status is null ? null : invoice => invoice.Status == status.Value)
            .And(envelopeState is null ? null : invoice => invoice.EnvelopeState == envelopeState.Value)
            .Build();
    }

    // where the signed invoice is kept: {tenant}/sales/{year}/{month} of the issue date, sales_{ettn}.xml
    private static string SignedPathOf(SalesInvoice invoice)
    {
        DateTime issued = invoice.IssueDate ?? throw new InvalidOperationException($"Invoice {invoice.InvoiceId} has no issue date.");
        return $"{invoice.TenantId:N}/sales/{issued:yyyy}/{issued:MM}/sales_{invoice.InvoiceId:N}.xml";
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static SalesInvoiceDto ToDto(SalesInvoice invoice, Sight sight) => new()
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
        Profile = invoice.Profile,
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
        EnvelopeId = invoice.EnvelopeId,
        EnvelopeState = invoice.EnvelopeState.ToString(),
        EnvelopeStatusCode = invoice.EnvelopeStatusCode,
        EnvelopeStatusText = invoice.EnvelopeStatusText,
        EnvelopeNote = invoice.EnvelopeNote,
        EnvelopeStatusAt = invoice.EnvelopeStatusAt,
        History = InvoiceHistoryMapper.ToDtos(invoice.History),
        Responses = InvoiceHistoryMapper.ToDtos(invoice.Responses),
        TenantId = invoice.TenantId,
        CreatedAt = invoice.CreatedAt,
        CreatedBy = invoice.CreatedBy,
        UpdatedAt = invoice.UpdatedAt,
        UpdatedBy = invoice.UpdatedBy,
    };

    private readonly record struct Sight(bool TaxIds, bool Amounts);
}
