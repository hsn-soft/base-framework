using System.Linq.Expressions;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Filters;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;
using Abc.OrderService.Application.Contracts.OrderDomain.Services;
using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.OrderService.Domain.OrderDomain.Repositories;
using Abc.Shared.Contracts.Events.Order;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace Abc.OrderService.Application.Services;

public sealed class OrderAppService(IRepository<Order, Guid> orders, IEventBus eventBus, IServiceProvider services) : ApplicationServiceBase(services), IOrderAppService
{
    public async Task<OrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<OrderDto>> GetPagedListAsync(GetOrdersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<Order> page = await orders.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.CustomerUserName, ParseStatus(pagedInput.Status)), OrderSorting.Fields, OrderSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<List<OrderDto>> GetFilterListAsync(GetOrdersFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Order> all = await orders.GetListAsync(
            filterInput.ToListOptions(Filter(null, filterInput.CustomerUserName, ParseStatus(filterInput.Status)), OrderSorting.Fields, OrderSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<OrderSearchDto>> GetSearchListAsync(GetOrdersSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Order> found = await orders.GetListAsync(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null), OrderSorting.Fields, OrderSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(order => new OrderSearchDto { Id = order.Id, CustomerUserName = order.CustomerUserName, Status = order.Status.ToString(), TotalAmount = order.TotalAmount })];
    }

    public async Task<OrderDto> CreateAsync(OrderCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        IEnumerable<OrderLine> lines = input.Lines.Select(line => OrderLine.Create(line.ProductId.GetValueOrDefault(), line.ProductName, line.UnitPrice.GetValueOrDefault(), line.Quantity.GetValueOrDefault()));
        Order order = Order.Create(CurrentUser.UserName ?? string.Empty, input.ShippingAddress, input.Note, lines);
        await orders.AddAsync(order, cancellationToken).ConfigureAwait(false);

        // the product service checks the stock of the order; the answer comes back as an event (there is no outbox yet: an order saved whose event was not published waits as Pending)
        await eventBus.PublishAsync(
            new OrderCreatedEto(order.Id, order.CustomerUserName, order.ShippingAddress, [.. order.Lines.Select(line => new OrderLineEto(line.ProductId, line.Quantity))]),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return ToDto(order);
    }

    public async Task UpdateAsync(OrderUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Order order = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        order.Update(ParseStatus(input.Status) ?? throw new DomainException("Status is required.").WithKey("status_required"), input.Note);
        await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order order = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await orders.DeleteAsync(order, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Order> FindAsync(Guid id, CancellationToken cancellationToken)
        => await orders.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Order, bool>> Filter(string? searchText, string? customerUserName, OrderStatus? status)
    {
        string? search = Normalized(searchText);
        string? customer = Normalized(customerUserName);
        return new FilterBuilder<Order>()
            .And(search is null ? null : order => order.NormalizedCustomerUserName.Contains(search))
            .And(customer is null ? null : order => order.NormalizedCustomerUserName.Contains(customer))
            .And(status is null ? null : order => order.Status == status.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static OrderStatus? ParseStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(status.Trim(), ignoreCase: true, out OrderStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException($"'{status}' is not a status of an order (Pending, Confirmed, Shipped, Delivered, Cancelled).").WithKey("order_status_invalid", status);

    private OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        CustomerUserName = order.CustomerUserName,
        Status = order.Status.ToString(),
        StatusText = L[$"OrderStatus:{order.Status}"].Value,
        ShippingAddress = order.ShippingAddress,
        Note = order.Note,
        TotalAmount = order.TotalAmount,
        Lines = [.. order.Lines.Select(line => new OrderLineDto { ProductId = line.ProductId, ProductName = line.ProductName, UnitPrice = line.UnitPrice, Quantity = line.Quantity, Total = line.Total })],
        TenantId = order.TenantId,
        CreatedAt = order.CreatedAt,
        CreatedBy = order.CreatedBy,
        UpdatedAt = order.UpdatedAt,
        UpdatedBy = order.UpdatedBy,
    };
}
