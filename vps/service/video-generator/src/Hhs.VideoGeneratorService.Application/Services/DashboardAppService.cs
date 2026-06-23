using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain;
using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.VideoGeneratorService.Application.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.Settings;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Repositories;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class DashboardAppService : ApplicationServiceBase, IDashboardAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IVideoRequestRepository _repository;
    private readonly VideoRequestQuerySettings _querySettings;


    public DashboardAppService(
        IServiceProvider provider,
        IVideoRequestRepository repository,
        IOptions<VideoRequestQuerySettings> querySettings
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _repository = repository;
        _querySettings = querySettings?.Value ?? throw new ArgumentNullException(nameof(querySettings));
    }

    public async Task<GetAnalysisVideoDetailResultDto> GetAnalysisVideoDetailResultDtoAsync(GetAnalysisVideoDetailRequest input, CancellationToken cancellationToken = default)
    {
        var analysisVideos = await _repository.GetListAsync(
            selector: u => u,
            options: new ListQueryOptions<VideoRequest>
            {
                Filter = x =>
                    x.RefContentType == ReferenceContentTypes.ANALYSIS_CONTENT
                    && x.CreationTime >= input.StartDate
                    && x.CreationTime <= input.EndDate,
                OrderByDynamic = $"{nameof(VideoRequest.CreationTime)} desc",
                MaxResultCount = _querySettings.AnalysisVideoMaxResultCount
            });

        return new GetAnalysisVideoDetailResultDto
        {
            TotalCount = analysisVideos?.Count ?? 0,
            Items = analysisVideos?.Select(v => new AnalysisVideoItemDto
            {
                Id = v.Id,
                RefContentId = v.RefContentId,
                OperationStatus = v.OperationStatus.ToString(),
                CreationTime = v.CreationTime,
                StorageVideoUrl = v.StorageVideoUrl,
                NormalizedContentDatas = v.NormalizedContentDatas?.Select(n => new NormalizedContentDataDto
                {
                    TitleText = n.TitleText,
                    NormalizedContent = n.NormalizedContent,
                    ImageUrl = n.ImageUrl
                }).ToList()
            }).ToList() ?? new List<AnalysisVideoItemDto>()
        };
    }
}