using HsnSoft.Base.Application.Dtos;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Filters;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Submits;

namespace Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Services;

/// <summary>What can be done with the video requests. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IVideoRequestAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such video request.</exception>
    Task<VideoRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<VideoRequestDto>> GetPagedListAsync(GetVideoRequestsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<VideoRequestDto> CreateAsync(VideoRequestCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such video request.</exception>
    Task UpdateAsync(VideoRequestUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such video request.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
