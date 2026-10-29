using System.Linq.Expressions;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Services;
using Abc.ShipmentService.Domain.PaymentDomain.Entities;
using Abc.ShipmentService.Domain.PaymentDomain.Exceptions;
using Abc.ShipmentService.Domain.PaymentDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;

namespace Abc.ShipmentService.Application.Services;

public sealed class PaymentAppService(IRepository<Payment, Guid> payments, IServiceProvider services) : ApplicationServiceBase(services), IPaymentAppService
{
    public async Task<PaymentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<PaymentDto>> GetPagedListAsync(GetPaymentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<Payment> page = await payments.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput.OrderId, pagedInput.CustomerId, ParseStatus(pagedInput.Status)), PaymentSorting.Fields, PaymentSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<List<PaymentDto>> GetFilterListAsync(GetPaymentsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Payment> all = await payments.GetListAsync(
            filterInput.ToListOptions(Filter(filterInput.OrderId, filterInput.CustomerId, ParseStatus(filterInput.Status)), PaymentSorting.Fields, PaymentSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<PaymentSearchDto>> GetSearchListAsync(GetPaymentsSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        // a payment is looked for by its order: the text is an order id
        Guid? orderId = Guid.TryParse(searchInput.SearchText?.Trim(), out Guid parsed) ? parsed : null;
        IReadOnlyList<Payment> found = await payments.GetListAsync(
            searchInput.ToListOptions(Filter(orderId, null, null), PaymentSorting.Fields, PaymentSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(payment => new PaymentSearchDto { Id = payment.Id, OrderId = payment.OrderId, Amount = payment.Amount, Status = payment.Status.ToString() })];
    }

    public async Task<PaymentDto> CreateAsync(PaymentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Payment payment = Payment.Create(input.OrderId.GetValueOrDefault(), input.CustomerId.GetValueOrDefault(), input.Amount.GetValueOrDefault(), input.Currency);
        await payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        return ToDto(payment);
    }

    public async Task UpdateAsync(PaymentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Payment payment = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        payment.ChangeStatus(ParseStatus(input.Status) ?? throw new DomainException("Status is required.").WithKey("status_required"), input.Reason);
        await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await payments.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<Payment> FindAsync(Guid id, CancellationToken cancellationToken)
        => await payments.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw PaymentNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Payment, bool>> Filter(Guid? orderId, Guid? customerId, PaymentStatus? status)
        => new FilterBuilder<Payment>()
            .And(orderId is null ? null : payment => payment.OrderId == orderId.Value)
            .And(customerId is null ? null : payment => payment.CustomerId == customerId.Value)
            .And(status is null ? null : payment => payment.Status == status.Value)
            .Build();

    private static PaymentStatus? ParseStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(status.Trim(), ignoreCase: true, out PaymentStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException($"'{status}' is not a status of a payment (Pending, Succeeded, Failed, Refunded).").WithKey("payment_status_invalid", status);

    private PaymentDto ToDto(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        CustomerId = payment.CustomerId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status.ToString(),
        StatusText = L[$"PaymentStatus:{payment.Status}"].Value,
        Reference = payment.Reference,
        FailureReason = payment.FailureReason,
        CreatedAt = payment.CreatedAt,
        CreatedBy = payment.CreatedBy,
        UpdatedAt = payment.UpdatedAt,
        UpdatedBy = payment.UpdatedBy,
    };
}
