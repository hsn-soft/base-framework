using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Services;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Exceptions;
using Edx.InvoiceService.Domain.InvoiceReportDomain.Repositories;
using Edx.Shared.Helper;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Edx.InvoiceService.Application.Services;

public sealed class InvoiceReportAppService(IRepository<InvoiceReport, Guid> reports, ISalesInvoiceRepository invoices, IPermissionChecker permissions, TimeProvider time, IServiceProvider services)
    : ApplicationService(services), IInvoiceReportAppService
{
    public async Task<InvoiceReportDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // the constraint first: a caller without it is refused before the store is asked
        DateTimeOffset since = await SinceAsync(cancellationToken).ConfigureAwait(false);
        InvoiceReport report = await reports.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw InvoiceReportNotFound.For(id);
        return report.GeneratedAt >= since ? ToDto(report, await CanSeeTotalAsync(cancellationToken).ConfigureAwait(false)) : throw InvoiceReportNotFound.For(id);
    }

    public async Task<PagedDataResultDto<InvoiceReportDto>> GetPagedListAsync(GetInvoiceReportsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        DateTimeOffset since = await SinceAsync(cancellationToken).ConfigureAwait(false);
        PagedQueryResult<InvoiceReport> page = await reports.GetPagedListAsync(
            pagedInput.ToPagedOptions<InvoiceReport>(report => report.GeneratedAt >= since, InvoiceReportSorting.Fields, InvoiceReportSorting.Default), cancellationToken).ConfigureAwait(false);
        bool total = await CanSeeTotalAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, report => ToDto(report, total));
    }

    public async Task<InvoiceReportDto> GenerateAsync(InvoiceReportGenerateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        int maxDays = await permissions.RequireIntAsync(EdxConstraintPermissions.InvoiceReport.MaxDays, cancellationToken).ConfigureAwait(false);
        DateTime start = input.PeriodStart.GetValueOrDefault().Date;
        DateTime end = input.PeriodEnd.GetValueOrDefault().Date;
        int days = (int)(end - start).TotalDays + 1;
        if (days > maxDays)
        {
            throw new InvoiceReportPeriodOverLimitException(days, maxDays);
        }

        // what the invoices of the period say: all of the tenant's, whatever the caller may see of them one by one (a report is a summary, not a list). A system with many invoices asks the store to add them up; the sample reads the two fields it needs.
        string currency = input.Currency.Trim().ToUpperInvariant();
        IReadOnlyList<(decimal Total, SalesInvoiceStatus Status)> found = [.. (await invoices.GetListAsync(
            new ListQueryOptions<SalesInvoice> { Filter = invoice => invoice.IsLive && invoice.IssueDate >= start && invoice.IssueDate <= end && invoice.Currency == currency },
            invoice => new { invoice.Total, invoice.Status },
            cancellationToken).ConfigureAwait(false)).Select(item => (item.Total.GetValueOrDefault(), item.Status))];

        // the invoices that are live (a draft, approved or signed) count; the approved ones are those that were approved, signed or not yet
        InvoiceReport report = InvoiceReport.Generate(
            start, end, currency, found.Count, found.Count(item => item.Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Signed), found.Sum(item => item.Total), time.GetUtcNow());
        await reports.AddAsync(report, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Invoice report {InvoiceReportId} was made for {ReportPeriod}: {InvoiceCount} invoices", report.Id, report.Name, report.InvoiceCount);
        return ToDto(report, await CanSeeTotalAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        InvoiceReport report = await reports.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw InvoiceReportNotFound.For(id);
        await reports.DeleteAsync(report, cancellationToken).ConfigureAwait(false);
    }

    // the oldest a report may be for the caller to see it: now minus the days its constraint gives
    private async Task<DateTimeOffset> SinceAsync(CancellationToken cancellationToken)
        => time.GetUtcNow().AddDays(-await permissions.RequireIntAsync(EdxConstraintPermissions.InvoiceReport.MaxDays, cancellationToken).ConfigureAwait(false));

    private Task<bool> CanSeeTotalAsync(CancellationToken cancellationToken) => permissions.IsGrantedAsync(EdxOperationPermissions.InvoiceReport.TotalView, cancellationToken);

    private static InvoiceReportDto ToDto(InvoiceReport report, bool canSeeTotal) => new()
    {
        Id = report.Id,
        Name = report.Name,
        PeriodStart = report.PeriodStart,
        PeriodEnd = report.PeriodEnd,
        Currency = report.Currency,
        InvoiceCount = report.InvoiceCount,
        ApprovedCount = report.ApprovedCount,
        TotalAmount = canSeeTotal ? report.TotalAmount : null,
        GeneratedAt = report.GeneratedAt,
        TenantId = report.TenantId,
        CreatedAt = report.CreatedAt,
        CreatedBy = report.CreatedBy,
    };
}
