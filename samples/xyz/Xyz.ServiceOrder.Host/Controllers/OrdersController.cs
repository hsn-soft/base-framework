using HsnSoft.Base;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceOrder.Host.Domain;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared;
using Xyz.Shared.Contracts;
using Xyz.Shared.Events;

namespace Xyz.ServiceOrder.Host.Controllers;

public sealed record OrderResponse(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal? TotalPrice,
    OrderStatus Status,
    string? RejectReason,
    string? TrackingNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static OrderResponse From(Order order) => new(
        order.Id, order.ProductId, order.Quantity, order.TotalPrice, order.Status,
        order.RejectReason, order.TrackingNumber, order.CreatedAt, order.UpdatedAt);
}

[ApiController]
[Route("api/orders")]
[Authorize(Policy = BasePolicies.Authenticated)]
public sealed class OrdersController(OrderDbContext db, IEventBus eventBus, IDistributedCache cache) : ControllerBase
{
    /// <summary>
    /// Saves the order as Pending and announces it. The answer is "202 Accepted": whether the stock is enough is decided
    /// later by the product service. A client that retries with the same Idempotency-Key gets the first order back.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceAsync(
        PlaceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("The quantity must be greater than zero.");
        }

        // failure demo: a bug nobody expected (an object that is null), to see what the logs say (see samples/README.md)
        if (request.Quantity == FailureDemo.RequestQuantity)
        {
            Dictionary<string, string>? limits = null;
            _ = limits!.Count;
        }

        string customerId = User.GetRequiredUserId();
        string? cacheKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : $"idempotency:{customerId}:{idempotencyKey}";

        if (cacheKey is not null && await cache.GetStringAsync(cacheKey, cancellationToken) is { } existingId
            && await db.Orders.FindAsync([Guid.Parse(existingId)], cancellationToken) is { } existing)
        {
            return Accepted($"/api/orders/{existing.Id}", OrderResponse.From(existing));
        }

        var order = Order.Place(customerId, request.ProductId, request.Quantity);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        if (cacheKey is not null)
        {
            await cache.SetStringAsync(cacheKey, order.Id.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, cancellationToken);
        }

        await eventBus.PublishAsync(new OrderCreated(order.Id, order.ProductId, order.Quantity), cancellationToken: cancellationToken);

        return Accepted($"/api/orders/{order.Id}", OrderResponse.From(order));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Order order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        // a customer sees only their own orders; an admin sees all
        if (!User.IsInRole(Roles.Admin) && order.CustomerId != User.GetRequiredUserId())
        {
            throw new NotFoundException($"Order {id} was not found.");
        }

        return Ok(OrderResponse.From(order));
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        IQueryable<Order> query = db.Orders.AsNoTracking();
        if (!User.IsInRole(Roles.Admin))
        {
            string customerId = User.GetRequiredUserId();
            query = query.Where(x => x.CustomerId == customerId);
        }

        List<Order> orders = await query.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return Ok(orders.Select(OrderResponse.From));
    }
}
