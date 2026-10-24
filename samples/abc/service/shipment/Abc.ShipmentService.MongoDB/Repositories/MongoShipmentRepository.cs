using System.Text.RegularExpressions;
using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using Abc.ShipmentService.Domain.ShipmentDomain.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Abc.ShipmentService.MongoDB.Repositories;

public sealed class MongoShipmentRepository(IMongoDatabase database) : IShipmentRepository
{
    private IMongoCollection<Shipment> Shipments => database.GetCollection<Shipment>(ShipmentConsts.CollectionName);

    public async Task<Shipment?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => await Shipments.Find(shipment => shipment.Id == id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Shipment>> GetListAsync(ShipmentFilter filter, int skip, int take, CancellationToken cancellationToken = default)
        => await Shipments.Find(Filter(filter)).SortByDescending(shipment => shipment.CreatedAt).Skip(skip).Limit(take).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<long> CountAsync(ShipmentFilter filter, CancellationToken cancellationToken = default)
        => Shipments.CountDocumentsAsync(Filter(filter), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<Shipment>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default)
    {
        FilterDefinitionBuilder<Shipment> builder = Builders<Shipment>.Filter;
        FilterDefinition<Shipment> filter = builder.Empty;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            BsonRegularExpression part = Part(searchText);
            filter = builder.Or(builder.Regex(shipment => shipment.TrackingNumber, part), builder.Regex(shipment => shipment.NormalizedCarrier, part));
        }

        return await Shipments.Find(filter).SortByDescending(shipment => shipment.CreatedAt).Limit(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
        => Shipments.InsertOneAsync(shipment, cancellationToken: cancellationToken);

    public Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
        => Shipments.ReplaceOneAsync(stored => stored.Id == shipment.Id, shipment, cancellationToken: cancellationToken);

    public Task DeleteAsync(Shipment shipment, CancellationToken cancellationToken = default)
        => Shipments.DeleteOneAsync(stored => stored.Id == shipment.Id, cancellationToken);

    private static BsonRegularExpression Part(string text) => new(Regex.Escape(Shipment.Normalize(text)));

    private static FilterDefinition<Shipment> Filter(ShipmentFilter filter)
    {
        FilterDefinitionBuilder<Shipment> builder = Builders<Shipment>.Filter;
        List<FilterDefinition<Shipment>> parts = [];
        if (filter.OrderId is { } orderId)
        {
            parts.Add(builder.Eq(shipment => shipment.OrderId, orderId));
        }

        if (!string.IsNullOrWhiteSpace(filter.TrackingNumber))
        {
            parts.Add(builder.Regex(shipment => shipment.TrackingNumber, Part(filter.TrackingNumber)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Carrier))
        {
            parts.Add(builder.Regex(shipment => shipment.NormalizedCarrier, Part(filter.Carrier)));
        }

        if (filter.Status is { } status)
        {
            parts.Add(builder.Eq(shipment => shipment.Status, status));
        }

        return parts.Count == 0 ? builder.Empty : builder.And(parts);
    }
}
