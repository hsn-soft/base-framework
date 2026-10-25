using HsnSoft.Base.Application.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Submits;

namespace Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Services;

/// <summary>What can be done with the history of the shipments. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IShipmentHistoryAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task<ShipmentHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the records that match the filters, the latest first unless a sorting is given, with how many match in all.</summary>
    Task<PagedDataResultDto<ShipmentHistoryDto>> GetPagedListAsync(GetShipmentHistoriesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the records that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<ShipmentHistoryDto>> GetFilterListAsync(GetShipmentHistoriesFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The records that have the text in their tracking number or outcome, shown briefly.</summary>
    Task<List<ShipmentHistorySearchDto>> GetSearchListAsync(GetShipmentHistoriesSearch searchInput, CancellationToken cancellationToken = default);

    Task<ShipmentHistoryDto> CreateAsync(ShipmentHistoryCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task UpdateAsync(ShipmentHistoryUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
