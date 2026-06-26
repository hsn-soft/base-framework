using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Application.Contracts.JobDomain;
using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using Hhs.ContentService.Domain.SettingDomain.Entities;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class JobAppService(
    IServiceProvider provider,
    ICustomerVpSettingRepository customerVpSettingRepository
) : ApplicationServiceBase(provider), IJobAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task AnalysisVideoGenerationQueryTriggerAsync(AnalysisVideoGenerationQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "ANALYSIS_VIDEO_GENERATION_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        var scopeKeys = await customerVpSettingRepository.GetListAsync(new ListQueryOptions<CustomerVpSetting>
            {
                Filter = x
                    => !x.IsBlocked && x.DailyAnalysisVideoGenerationLimit > 0
            }
            , selector: x => x.ScopeKey);
        if (scopeKeys is { Count: > 0 })
        {
            foreach (string scopeKey in scopeKeys)
            {
                await EventBus.PublishAsync(
                    eventMessage: new AnalysisVideoGenerationQueryEto(ScopeKey: scopeKey),
                    correlationId: correlationId
                );
            }
        }
        else
        {
            _logger.LogWarning("There is no client which has ANALYSIS video generation limit");
        }
    }
}