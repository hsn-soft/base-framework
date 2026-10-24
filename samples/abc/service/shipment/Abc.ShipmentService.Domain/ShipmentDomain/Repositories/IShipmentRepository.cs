using Abc.ShipmentService.Domain.ShipmentDomain.Entities;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Repositories;

/// <summary>Where the shipments are kept. The domain says what it needs; the persistence project says how.</summary>
public interface IShipmentRepository
{
    Task<Shipment?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The shipments that match <paramref name="filter"/>, the newest first, from <paramref name="skip"/> on, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<Shipment>> GetListAsync(ShipmentFilter filter, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>How many shipments match <paramref name="filter"/>.</summary>
    Task<long> CountAsync(ShipmentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>The shipments whose tracking number or carrier has <paramref name="searchText"/> in it (all of them when it is empty), the newest first, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<Shipment>> SearchAsync(string? searchText, int limit, CancellationToken cancellationToken = default);

    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task DeleteAsync(Shipment shipment, CancellationToken cancellationToken = default);
}
