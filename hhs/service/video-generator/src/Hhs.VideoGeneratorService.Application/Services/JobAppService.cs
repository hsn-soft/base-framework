using Hhs.VideoGeneratorService.Application.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.Settings;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class JobAppService : ApplicationServiceBase, IJobAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IVideoRequestRepository _repository;
    private readonly VideoRequestQuerySettings _querySettings;


    public JobAppService(
        IServiceProvider provider,
        IVideoRequestRepository repository,
        IOptions<VideoRequestQuerySettings> querySettings
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _repository = repository;
        _querySettings = querySettings?.Value ?? throw new ArgumentNullException(nameof(querySettings));
    }

    public async Task VideoRequestQueryTriggerAsync(VideoRequestQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "VIDEO_REQUEST_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        var waitGenerationVideoRequestIdList = await _repository.GetListAsync(
            selector: u => new { u.Id },
            options: new ListQueryOptions<VideoRequest>
            {
                Filter = x =>
                    x.OperationStatus == VideoRequestStates.VideoSentWaitForVideoGeneration
                    && x.LastQueryTime < DateTime.UtcNow.AddSeconds(-1 * _querySettings.ReQueryWaitPeriodForSeconds),
                OrderByDynamic = $"{nameof(VideoRequest.LastQueryTime)} asc",
                MaxResultCount = _querySettings.QueryPackageCount
            });

        if (waitGenerationVideoRequestIdList is { Count: > 0 })
        {
            foreach (var item in waitGenerationVideoRequestIdList)
            {
                await EventBus.PublishAsync(eventMessage: new VideoRequestQueryEto(VideoRequestId: item.Id), correlationId: correlationId);
            }
        }


    }
}