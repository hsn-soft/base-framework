using System.Linq.Expressions;
using System.Xml.Linq;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Services;
using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Exceptions;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
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

/// <summary>The purchase invoices of a tenant, with the same permissions the invoices have: the limit of what the caller may see (constraint <c>max-view-amount</c>, which a caller must have been given), the amounts (an operation) and the XML (both operations).</summary>
public sealed class PurchaseInvoiceAppService(IPurchaseInvoiceRepository invoices, IBlobStore blobs, IPermissionChecker permissions, IOptions<InvoiceStorageOptions> storage, IEventBus eventBus, TimeProvider time, IServiceProvider services)
    : ApplicationService(services), IPurchaseInvoiceAppService
{
    public async Task<PurchaseInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PurchaseInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return invoice.PayableAmount <= limit ? ToDto(invoice, await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.PriceView, cancellationToken).ConfigureAwait(false)) : throw PurchaseInvoiceNotFound.For(id);
    }

    public async Task<PagedDataResultDto<PurchaseInvoiceDto>> GetPagedListAsync(GetPurchaseInvoicesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PagedQueryResult<PurchaseInvoice> page = await invoices.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput, limit), PurchaseInvoiceSorting.Fields, PurchaseInvoiceSorting.Default), cancellationToken).ConfigureAwait(false);
        bool amounts = await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.PriceView, cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, invoice => ToDto(invoice, amounts));
    }

    public async Task<InvoiceXmlDto> GetXmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        foreach (string operation in new[] { EdxOperationPermissions.Invoice.TaxIdView, EdxOperationPermissions.Invoice.PriceView })
        {
            if (!await permissions.IsGrantedAsync(operation, cancellationToken).ConfigureAwait(false))
            {
                throw new BaseHttpException(StatusCodes.Status403Forbidden, $"The permission '{operation}' is required to read the XML of an invoice.").WithKey("operation_not_granted", operation);
            }
        }

        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PurchaseInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (invoice.PayableAmount > limit)
        {
            throw PurchaseInvoiceNotFound.For(id);
        }

        Stream stream = await blobs.OpenReadAsync(storage.Value.Container, invoice.XmlPath, cancellationToken).ConfigureAwait(false) ?? throw PurchaseInvoiceNotFound.For(id);
        await using (stream.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            return new InvoiceXmlDto { FileName = $"purchase_{invoice.InvoiceUuid:N}.xml", Content = copy.ToArray() };
        }
    }

    public async Task<PurchaseInvoiceDto> RespondAsync(Guid id, PurchaseInvoiceRespondDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        decimal limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxViewAmount, cancellationToken).ConfigureAwait(false);
        PurchaseInvoice invoice = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (invoice.PayableAmount > limit)
        {
            throw PurchaseInvoiceNotFound.For(id);
        }

        if (invoice.Profile != UblInvoiceBuilder.CommercialProfileId)
        {
            throw new BusinessException("purchase_invoice_not_commercial", $"Only a commercial invoice ({UblInvoiceBuilder.CommercialProfileId}) is answered; this one is {invoice.Profile}.").WithKey("purchase_invoice_not_commercial", invoice.Profile);
        }

        if (!invoice.CanBeAnswered)
        {
            throw new BusinessException("purchase_invoice_already_answered", "This invoice was answered already (or its answer is being made).").WithKey("purchase_invoice_already_answered");
        }

        string? note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        if (!input.Accept && note is null)
        {
            throw new BusinessException("purchase_invoice_refusal_needs_reason", "A reason is needed to refuse an invoice.").WithKey("purchase_invoice_refusal_needs_reason");
        }

        EInvoiceProfile profile = EdxEInvoiceProfiles.ForTenant(invoice.TenantId) ?? throw new InvalidOperationException($"The tenant {invoice.TenantId} has no e-invoice account.");
        DateTimeOffset now = time.GetUtcNow();
        Guid answerUuid = Guid.NewGuid();
        UblAnswerKind kind = input.Accept ? UblAnswerKind.Accepted : UblAnswerKind.Rejected;
        UblInvoiceAnswer answer = new(
            UblInvoiceAnswer.NumberFor(answerUuid, now.LocalDateTime),
            answerUuid,
            now.LocalDateTime,
            kind,
            note,
            new UblParty(profile.RegisterNumber, profile.Title, profile.TaxOffice, profile.District, profile.City, profile.Country),
            new UblParty(invoice.SupplierRegisterNumber, invoice.SupplierName, null, "-", "-", "Türkiye"),
            invoice.Number,
            invoice.InvoiceUuid,
            invoice.IssueDate);
        XElement document = answer.ToElement();
        IReadOnlyList<string> problems = UblSchemas.Default.Validate(document);
        if (problems.Count > 0)
        {
            throw new InvalidOperationException($"The answer to invoice {invoice.InvoiceUuid} does not follow the UBL-TR schema: {string.Join(" | ", problems.Take(3))}");
        }

        // the unsigned answer is kept until the signer signs it; the signed one goes beside the invoice
        string draftPath = InvoicePaths.PurchaseAnswerDraft(invoice.TenantId, invoice.InvoiceUuid);
        string signedPath = InvoicePaths.PurchaseAnswer(invoice.TenantId, invoice.IssueDate, invoice.InvoiceUuid);
        using (MemoryStream stream = new(UblXml.ToBytes(document), writable: false))
        {
            await blobs.SaveAsync(storage.Value.Container, draftPath, stream, "application/xml", cancellationToken).ConfigureAwait(false);
        }

        invoice.RequestAnswer(kind.ToString(), now, CurrentUser.Id, note);
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(
            new PurchaseAnswerRequestedEto(
                invoice.InvoiceUuid, invoice.Number, answerUuid, answer.Number, kind.ToString(), note, invoice.SupplierRegisterNumber, invoice.SupplierName, invoice.EnvelopeId, profile.Title, storage.Value.Container, draftPath, signedPath),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Purchase invoice {InvoiceUuid} ({InvoiceNumber}) was answered: {AnswerKind}", invoice.InvoiceUuid, invoice.Number, kind);
        return ToDto(invoice, await permissions.IsGrantedAsync(EdxOperationPermissions.Invoice.PriceView, cancellationToken).ConfigureAwait(false));
    }

    private async Task<PurchaseInvoice> FindAsync(Guid id, CancellationToken cancellationToken)
        => await invoices.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw PurchaseInvoiceNotFound.For(id);

    private static Expression<Func<PurchaseInvoice, bool>> Filter(GetPurchaseInvoicesPaged input, decimal limit)
    {
        string? number = string.IsNullOrWhiteSpace(input.Number) ? null : StringHelper.Normalize(input.Number);
        string? supplier = string.IsNullOrWhiteSpace(input.SupplierName) ? null : input.SupplierName.Trim();
        return new FilterBuilder<PurchaseInvoice>()
            .And(invoice => invoice.PayableAmount <= limit)
            .And(number is null ? null : invoice => invoice.NormalizedNumber.Contains(number))
            .And(supplier is null ? null : invoice => invoice.SupplierName.Contains(supplier))
            .Build();
    }

    private static PurchaseInvoiceDto ToDto(PurchaseInvoice invoice, bool amounts) => new()
    {
        Id = invoice.Id,
        InvoiceUuid = invoice.InvoiceUuid,
        EnvelopeId = invoice.EnvelopeId,
        Number = invoice.Number,
        IssueDate = invoice.IssueDate,
        Currency = invoice.Currency,
        Profile = invoice.Profile,
        SupplierRegisterNumber = invoice.SupplierRegisterNumber,
        SupplierName = invoice.SupplierName,
        CustomerRegisterNumber = invoice.CustomerRegisterNumber,
        CustomerName = invoice.CustomerName,
        LineCount = invoice.LineCount,
        TaxExclusiveAmount = amounts ? invoice.TaxExclusiveAmount : null,
        TaxInclusiveAmount = amounts ? invoice.TaxInclusiveAmount : null,
        PayableAmount = amounts ? invoice.PayableAmount : null,
        ReceivedAt = invoice.ReceivedAt,
        EnvelopeStatusCode = invoice.EnvelopeStatusCode,
        EnvelopeStatusText = invoice.EnvelopeStatusText,
        EnvelopeStatusAt = invoice.EnvelopeStatusAt,
        History = InvoiceHistoryMapper.ToDtos(invoice.History),
        Responses = InvoiceHistoryMapper.ToDtos(invoice.Responses),
        TenantId = invoice.TenantId,
        CreatedAt = invoice.CreatedAt,
        CreatedBy = invoice.CreatedBy,
        UpdatedAt = invoice.UpdatedAt,
        UpdatedBy = invoice.UpdatedBy,
    };
}
