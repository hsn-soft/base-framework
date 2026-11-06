using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Services;

/// <summary>What can be done with the analysis content items. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IAnalysisContentItemAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content item.</exception>
    Task<AnalysisContentItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AnalysisContentItemDto>> GetPagedListAsync(GetAnalysisContentItemsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<AnalysisContentItemDto> CreateAsync(AnalysisContentItemCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content item.</exception>
    Task UpdateAsync(AnalysisContentItemUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content item.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
