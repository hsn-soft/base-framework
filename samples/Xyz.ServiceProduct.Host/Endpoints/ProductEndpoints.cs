using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Xyz.ServiceProduct.Host.Domain;
using Xyz.ServiceProduct.Host.Persistence;
using Xyz.Shared.Hosting;

namespace Xyz.ServiceProduct.Host.Endpoints;

public sealed record CreateProductRequest(string Name, string Sku, decimal UnitPrice, int Stock);

public sealed record AdjustStockRequest(int Delta);

public static class ProductEndpoints
{
    public static string CacheKey(Guid id) => $"product:{id}";

    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").RequireAuthorization(ServiceRegister.AuthenticatedPolicy);

        group.MapGet("/", async (ProductStore store, CancellationToken cancellationToken) => Results.Ok(await store.ListAsync(cancellationToken)));
        group.MapGet("/{id:guid}", GetProductAsync);
        group.MapPost("/", CreateProductAsync).RequireAuthorization(ServiceRegister.AdminPolicy);
        group.MapPut("/{id:guid}/stock", AdjustStockAsync).RequireAuthorization(ServiceRegister.AdminPolicy);
    }

    /// <summary>Cache-aside read: redis first, the database on a miss. The entry is dropped whenever the stock changes.</summary>
    private static async Task<IResult> GetProductAsync(Guid id, ProductStore store, IDistributedCache cache, CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(CacheKey(id), cancellationToken);
        if (cached is not null)
        {
            return Results.Ok(JsonSerializer.Deserialize<Product>(cached, JsonSerializerOptions.Web));
        }

        var product = await store.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"Product {id} was not found.");
        await cache.SetStringAsync(
            CacheKey(id),
            JsonSerializer.Serialize(product, JsonSerializerOptions.Web),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
            cancellationToken);
        return Results.Ok(product);
    }

    private static async Task<IResult> CreateProductAsync(CreateProductRequest request, ProductStore store, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku) || request.UnitPrice < 0 || request.Stock < 0)
        {
            throw new BusinessRuleException("Name and Sku are required; the price and the stock cannot be negative.");
        }

        var product = new Product { Name = request.Name, Sku = request.Sku, UnitPrice = request.UnitPrice, Stock = request.Stock };
        await store.InsertAsync(product, cancellationToken);
        return Results.Created($"/api/products/{product.Id}", product);
    }

    private static async Task<IResult> AdjustStockAsync(Guid id, AdjustStockRequest request, ProductStore store, IDistributedCache cache, CancellationToken cancellationToken)
    {
        var product = await store.AdjustStockAsync(id, request.Delta, cancellationToken)
                      ?? throw new BusinessRuleException("The product does not exist or the stock would become negative.");
        await cache.RemoveAsync(CacheKey(id), cancellationToken);
        return Results.Ok(product);
    }
}
