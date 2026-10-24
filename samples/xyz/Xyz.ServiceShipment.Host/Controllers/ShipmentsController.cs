using System.Text.Json;
using HsnSoft.Base;
using HsnSoft.Base.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceShipment.Host.Domain;
using Xyz.ServiceShipment.Host.Persistence;

namespace Xyz.ServiceShipment.Host.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize(Policy = BasePolicies.Authenticated)]
public sealed class ShipmentsController(ShipmentStore store, IDistributedCache cache) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
        => Ok(await store.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"Shipment {id} was not found."));

    /// <summary>Tracking lookups are read far more often than shipments change, so they are cached in redis.</summary>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        string key = $"shipment-by-order:{orderId}";
        if (await cache.GetStringAsync(key, cancellationToken) is { } cached)
        {
            return Ok(JsonSerializer.Deserialize<Shipment>(cached, JsonSerializerOptions.Web));
        }

        Shipment shipment = await store.FindByOrderAsync(orderId, cancellationToken)
                       ?? throw new NotFoundException($"There is no shipment for order {orderId}.");
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(shipment, JsonSerializerOptions.Web),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);
        return Ok(shipment);
    }
}
