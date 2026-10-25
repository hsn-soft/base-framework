using HsnSoft.Base.Application.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Services;

/// <summary>What can be done with the shipments. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IShipmentAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such shipment.</exception>
    Task<ShipmentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the shipments that match the filters, with how many match in all.</summary>
    Task<PagedDataResultDto<ShipmentDto>> GetPagedListAsync(GetShipmentsPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the shipments that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<ShipmentDto>> GetFilterListAsync(GetShipmentsFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The shipments that have the text in them, shown briefly.</summary>
    Task<List<ShipmentSearchDto>> GetSearchListAsync(GetShipmentsSearch searchInput, CancellationToken cancellationToken = default);

    Task<ShipmentDto> CreateAsync(ShipmentCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such shipment.</exception>
    Task UpdateAsync(ShipmentUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such shipment.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
