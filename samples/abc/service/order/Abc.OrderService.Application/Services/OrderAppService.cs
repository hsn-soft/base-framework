using Abc.OrderService.Application.Contracts.Dtos;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Filters;
using Abc.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;
using Abc.OrderService.Application.Contracts.OrderDomain.Services;
using Abc.OrderService.Domain.OrderDomain.Entities;
using Abc.OrderService.Domain.OrderDomain.Exceptions;
using Abc.OrderService.Domain.OrderDomain.Repositories;
using HsnSoft.Base;

using HsnSoft.Base.Security;

namespace Abc.OrderService.Application.Services;

public sealed class OrderAppService(IOrderRepository orders, TimeProvider clock, ICurrentUser currentUser) : IOrderAppService
{
    public async Task<OrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<OrderDto>> GetPagedListAsync(GetOrdersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        OrderFilter filter = new(pagedInput.CustomerUserName, ParseStatus(pagedInput.Status));
        IReadOnlyList<Order> page = await orders.GetListAsync(filter, pagedInput.Skip, pagedInput.Take, cancellationToken).ConfigureAwait(false);
        int total = await orders.CountAsync(filter, cancellationToken).ConfigureAwait(false);
        return new PagedDataResultDto<OrderDto> { Items = [.. page.Select(ToDto)], TotalCount = total, Skip = pagedInput.Skip, Take = pagedInput.Take };
    }

    public async Task<List<OrderDto>> GetFilterListAsync(GetOrdersFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        OrderFilter filter = new(filterInput.CustomerUserName, ParseStatus(filterInput.Status));
        IReadOnlyList<Order> all = await orders.GetListAsync(filter, 0, filterInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<OrderSearchDto>> GetSearchListAsync(GetOrdersSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Order> found = await orders.SearchAsync(searchInput.SearchText, searchInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. found.Select(order => new OrderSearchDto { Id = order.Id, CustomerUserName = order.CustomerUserName, Status = order.Status.ToString(), TotalAmount = order.TotalAmount })];
    }

    public async Task<OrderDto> CreateAsync(OrderCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        IEnumerable<OrderLine> lines = input.Lines.Select(line => OrderLine.Create(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity));
        Order order = Order.Create(currentUser.UserId ?? string.Empty, input.Note, lines, clock.GetUtcNow(), currentUser.UserId);
        await orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
        return ToDto(order);
    }

    public async Task UpdateAsync(OrderUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Order order = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        order.Update(ParseStatus(input.Status) ?? throw new DomainException("Status is required."), input.Note, clock.GetUtcNow(), currentUser.UserId);
        await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Order order = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await orders.DeleteAsync(order, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Order> FindAsync(Guid id, CancellationToken cancellationToken)
        => await orders.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw OrderNotFound.For(id);

    private static OrderStatus? ParseStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(status.Trim(), ignoreCase: true, out OrderStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException($"'{status}' is not a status of an order (Pending, Confirmed, Shipped, Delivered, Cancelled).");

    private static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        CustomerUserName = order.CustomerUserName,
        Status = order.Status.ToString(),
        Note = order.Note,
        TotalAmount = order.TotalAmount,
        Lines = [.. order.Lines.Select(line => new OrderLineDto { ProductId = line.ProductId, ProductName = line.ProductName, UnitPrice = line.UnitPrice, Quantity = line.Quantity, Total = line.Total })],
        CreatedAt = order.CreatedAt,
        CreatedBy = order.CreatedBy,
        UpdatedAt = order.UpdatedAt,
        UpdatedBy = order.UpdatedBy,
    };
}
