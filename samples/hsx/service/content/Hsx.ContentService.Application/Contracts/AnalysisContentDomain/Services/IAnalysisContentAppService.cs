using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Submits;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Services;

/// <summary>What can be done with the analysis contents. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IAnalysisContentAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content.</exception>
    Task<AnalysisContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AnalysisContentDto>> GetPagedListAsync(GetAnalysisContentsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<AnalysisContentDto> CreateAsync(AnalysisContentCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content.</exception>
    Task UpdateAsync(AnalysisContentUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such analysis content.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
