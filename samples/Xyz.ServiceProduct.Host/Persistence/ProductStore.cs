using MongoDB.Driver;
using Xyz.ServiceProduct.Host.Domain;

namespace Xyz.ServiceProduct.Host.Persistence;

public sealed class ProductStore(IMongoDatabase database)
{
    // Fixed ids so the smoke test and the README can refer to the seeded products.
    public static readonly Guid KeyboardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MouseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IMongoCollection<Product> _products = database.GetCollection<Product>("products");
    private readonly IMongoCollection<StockReservation> _reservations = database.GetCollection<StockReservation>("stock_reservations");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _products.Indexes.CreateOneAsync(
            new CreateIndexModel<Product>(Builders<Product>.IndexKeys.Ascending(x => x.Sku), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        if (await _products.CountDocumentsAsync(FilterDefinition<Product>.Empty, cancellationToken: cancellationToken) == 0)
        {
            await _products.InsertManyAsync(
            [
                new Product { Id = KeyboardId, Name = "Mechanical Keyboard", Sku = "KEY-001", UnitPrice = 89.90m, Stock = 100 },
                new Product { Id = MouseId, Name = "Wireless Mouse", Sku = "MOU-001", UnitPrice = 39.90m, Stock = 3 },
            ], cancellationToken: cancellationToken);
        }
    }

    public async Task<List<Product>> ListAsync(CancellationToken cancellationToken)
        => await _products.Find(FilterDefinition<Product>.Empty).SortBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken)
        => await _products.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task InsertAsync(Product product, CancellationToken cancellationToken)
        => await _products.InsertOneAsync(product, cancellationToken: cancellationToken);

    /// <summary>Adds <paramref name="delta"/> to the stock (negative to remove). Returns the product, or null if it does not exist.</summary>
    public async Task<Product?> AdjustStockAsync(Guid id, int delta, CancellationToken cancellationToken)
        => await _products.FindOneAndUpdateAsync(
            Builders<Product>.Filter.Eq(x => x.Id, id) & Builders<Product>.Filter.Gte(x => x.Stock, delta < 0 ? -delta : 0),
            Builders<Product>.Update.Inc(x => x.Stock, delta),
            new FindOneAndUpdateOptions<Product> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

    /// <summary>
    /// Takes <paramref name="quantity"/> items out of the stock for an order. Safe to call again for the same order:
    /// the stored result is returned and the stock is not touched twice.
    /// </summary>
    public async Task<StockReservation> ReserveAsync(Guid orderId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var reservation = new StockReservation { OrderId = orderId, ProductId = productId, Quantity = quantity, Status = ReservationStatus.Processing };

        try
        {
            await _reservations.InsertOneAsync(reservation, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await _reservations.Find(x => x.OrderId == orderId).FirstAsync(cancellationToken);
        }

        // The filter and the decrement are one atomic operation: two orders can never both take the last item.
        var product = await _products.FindOneAndUpdateAsync(
            Builders<Product>.Filter.Eq(x => x.Id, productId) & Builders<Product>.Filter.Gte(x => x.Stock, quantity),
            Builders<Product>.Update.Inc(x => x.Stock, -quantity),
            new FindOneAndUpdateOptions<Product> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (product is not null)
        {
            reservation.Status = ReservationStatus.Reserved;
            reservation.UnitPrice = product.UnitPrice;
        }
        else
        {
            reservation.Status = ReservationStatus.Rejected;
            reservation.Reason = await FindAsync(productId, cancellationToken) is null ? "The product does not exist." : "Not enough stock.";
        }

        await _reservations.ReplaceOneAsync(x => x.OrderId == orderId, reservation, cancellationToken: cancellationToken);
        return reservation;
    }
}
