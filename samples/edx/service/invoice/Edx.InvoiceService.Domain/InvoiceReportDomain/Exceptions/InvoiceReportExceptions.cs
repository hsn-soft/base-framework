using HsnSoft.Base;

namespace Edx.InvoiceService.Domain.InvoiceReportDomain.Exceptions;

/// <summary>There is no such report (or it is another tenant's, or older than the caller may look): answered as 404.</summary>
public static class InvoiceReportNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Invoice report {id} was not found.").WithKey("invoice_report_not_found", id);
}

/// <summary>The period of the report is longer than the caller may look back (a known error: the limit is the constraint <c>constraint.invoice-report.max-days</c> its role was given).</summary>
public sealed class InvoiceReportPeriodOverLimitException : BusinessException
{
    public const string Code = "invoice_report_period_over_limit";

    public InvoiceReportPeriodOverLimitException(int days, int limit)
        : base(Code, $"A report of {days} days is more than the {limit} days you may look back.") => WithKey(Code, days, limit);
}
