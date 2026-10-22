using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceOrder.Host.Domain;
using Xyz.ServiceOrder.Host.Persistence;
using Xyz.Shared.Events;
using Xyz.Shared.Hosting;
using Xyz.Shared.Messaging;
using Xyz.Shared.Security;

namespace Xyz.ServiceOrder.Host.Endpoints;

public sealed record PlaceOrderRequest(Guid ProductId, int Quantity);

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

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").RequireAuthorization(ServiceRegister.AuthenticatedPolicy);

        group.MapPost("/", PlaceOrderAsync);
        group.MapGet("/", ListOrdersAsync);
        group.MapGet("/{id:guid}", GetOrderAsync);
    }

    /// <summary>
    /// Saves the order as Pending and announces it. The answer is "202 Accepted": whether the stock is enough is decided
    /// later by the product service. A client that retries with the same Idempotency-Key gets the first order back.
    /// </summary>
    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderRequest request,
        ClaimsPrincipal user,
        OrderDbContext db,
        IEventBus eventBus,
        IDistributedCache cache,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("The quantity must be greater than zero.");
        }

        string customerId = CustomerId(user);
        string? cacheKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : $"idempotency:{customerId}:{idempotencyKey}";

        if (cacheKey is not null && await cache.GetStringAsync(cacheKey, cancellationToken) is { } existingId
            && await db.Orders.FindAsync([Guid.Parse(existingId)], cancellationToken) is { } existing)
        {
            return Results.Accepted($"/api/orders/{existing.Id}", OrderResponse.From(existing));
        }

        var order = Order.Place(customerId, request.ProductId, request.Quantity);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        if (cacheKey is not null)
        {
            await cache.SetStringAsync(cacheKey, order.Id.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, cancellationToken);
        }

        await eventBus.PublishAsync(new OrderCreated(order.Id, order.ProductId, order.Quantity), cancellationToken);

        return Results.Accepted($"/api/orders/{order.Id}", OrderResponse.From(order));
    }

    private static async Task<IResult> GetOrderAsync(Guid id, ClaimsPrincipal user, OrderDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        // a customer sees only their own orders; an admin sees all
        if (!user.IsInRole(Roles.Admin) && order.CustomerId != CustomerId(user))
        {
            throw new NotFoundException($"Order {id} was not found.");
        }

        return Results.Ok(OrderResponse.From(order));
    }

    private static async Task<IResult> ListOrdersAsync(ClaimsPrincipal user, OrderDbContext db, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking();
        if (!user.IsInRole(Roles.Admin))
        {
            string customerId = CustomerId(user);
            query = query.Where(x => x.CustomerId == customerId);
        }

        var orders = await query.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return Results.Ok(orders.Select(OrderResponse.From));
    }

    private static string CustomerId(ClaimsPrincipal user)
        => user.FindFirstValue("sub") ?? throw new BusinessRuleException("The token has no subject.");
}
