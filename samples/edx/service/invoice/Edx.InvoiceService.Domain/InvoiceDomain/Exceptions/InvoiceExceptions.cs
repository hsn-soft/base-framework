using System.Globalization;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using HsnSoft.Base;

namespace Edx.InvoiceService.Domain.InvoiceDomain.Exceptions;

/// <summary>There is no such invoice (or it is another tenant's): answered as 404.</summary>
public static class InvoiceNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Invoice {id} was not found.").WithKey("invoice_not_found", id);
}

/// <summary>The invoice is not at the step the action needs (a known error: only a draft is approved or rejected).</summary>
public sealed class InvoiceStateException : BusinessException
{
    public const string Code = "invoice_wrong_state";

    public InvoiceStateException(Guid id, SalesInvoiceStatus status, string what)
        : base(Code, $"Invoice {id} is {status}: it cannot be {what} now.") => WithKey(Code, id, status.ToString(), what);
}

/// <summary>The invoice amounts to more than the caller may approve (a known error): the limit is a constraint the caller's role was given.</summary>
public sealed class InvoiceOverLimitException : BusinessException
{
    public const string Code = "invoice_total_over_limit";

    public InvoiceOverLimitException(decimal total, decimal limit)
        : base(Code, string.Create(CultureInfo.InvariantCulture, $"The invoice amounts to {total}, and you may not go over {limit}.")) => WithKey(Code, total, limit);
}
