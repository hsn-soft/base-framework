using System.Text.Json;
using HsnSoft.Base;
using HsnSoft.Base.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceProduct.Host.Domain;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared;

namespace Xyz.ServiceProduct.Host.Controllers;

public sealed record CreateProductRequest(string Name, string Sku, decimal UnitPrice, int Stock);

public sealed record AdjustStockRequest(int Delta);

[ApiController]
[Route("api/products")]
[Authorize(Policy = BasePolicies.Authenticated)]
public sealed class ProductsController(ProductStore store, IDistributedCache cache) : ControllerBase
{
    public static string CacheKey(Guid id) => $"product:{id}";

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken) => Ok(await store.ListAsync(cancellationToken));

    /// <summary>Cache-aside read: redis first, the database on a miss. The entry is dropped whenever the stock changes.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        string? cached = await cache.GetStringAsync(CacheKey(id), cancellationToken);
        if (cached is not null)
        {
            return Ok(JsonSerializer.Deserialize<Product>(cached, JsonSerializerOptions.Web));
        }

        Product product = await store.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"Product {id} was not found.");
        await cache.SetStringAsync(
            CacheKey(id),
            JsonSerializer.Serialize(product, JsonSerializerOptions.Web),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
            cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = ServiceRegister.AdminPolicy)]
    public async Task<IActionResult> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku) || request.UnitPrice < 0 || request.Stock < 0)
        {
            throw new BusinessRuleException("Name and Sku are required; the price and the stock cannot be negative.");
        }

        var product = new Product { Name = request.Name, Sku = request.Sku, UnitPrice = request.UnitPrice, Stock = request.Stock };
        await store.InsertAsync(product, cancellationToken);
        return Created($"/api/products/{product.Id}", product);
    }

    [HttpPut("{id:guid}/stock")]
    [Authorize(Policy = ServiceRegister.AdminPolicy)]
    public async Task<IActionResult> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        Product product = await store.AdjustStockAsync(id, request.Delta, cancellationToken)
                      ?? throw new BusinessRuleException("The product does not exist or the stock would become negative.");
        await cache.RemoveAsync(CacheKey(id), cancellationToken);
        return Ok(product);
    }
}
