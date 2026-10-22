using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceShipment.Host.Domain;
using Xyz.ServiceShipment.Host.Persistence;
using Xyz.Shared.Hosting;

namespace Xyz.ServiceShipment.Host.Endpoints;

public static class ShipmentEndpoints
{
    public static void MapShipmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/shipments").RequireAuthorization(ServiceRegister.AuthenticatedPolicy);

        group.MapGet("/{id:guid}", async (Guid id, ShipmentStore store, CancellationToken cancellationToken)
            => Results.Ok(await store.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"Shipment {id} was not found.")));

        group.MapGet("/by-order/{orderId:guid}", GetByOrderAsync);
    }

    /// <summary>Tracking lookups are read far more often than shipments change, so they are cached in redis.</summary>
    private static async Task<IResult> GetByOrderAsync(Guid orderId, ShipmentStore store, IDistributedCache cache, CancellationToken cancellationToken)
    {
        string key = $"shipment-by-order:{orderId}";
        if (await cache.GetStringAsync(key, cancellationToken) is { } cached)
        {
            return Results.Ok(JsonSerializer.Deserialize<Shipment>(cached, JsonSerializerOptions.Web));
        }

        var shipment = await store.FindByOrderAsync(orderId, cancellationToken)
                       ?? throw new NotFoundException($"There is no shipment for order {orderId}.");
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(shipment, JsonSerializerOptions.Web),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);
        return Results.Ok(shipment);
    }
}
