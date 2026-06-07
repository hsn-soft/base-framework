using Hhs.ContentService.Application.Contracts.Events;
using Hhs.ContentService.Application.Contracts.JobDomain;
using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ClientDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application.Services;

public sealed class JobAppService : ApplicationServiceBase, IJobAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IClientRepository _clientRepository;

    public JobAppService(
        IServiceProvider provider,
        IClientRepository clientRepository
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _clientRepository = clientRepository;
    }

    public async Task AnalysisVideoGenerationQueryTriggerAsync(AnalysisVideoGenerationQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "ANALYSIS_VIDEO_GENERATION_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        var clients = await _clientRepository.GetListAsync(new ListQueryOptions<Client> { Filter = x => !x.IsBlocked && x.DailyAnalysisVideoGenerationLimit > 0 });
        if (clients is { Count: > 0 })
        {
            foreach (var client in clients)
            {
                await EventBus.PublishAsync(eventMessage: new AnalysisVideoGenerationQueryEto(AppClientId: client.Id), correlationId: correlationId);
            }
        }
        else
        {
            _logger.LogWarning("There is no client which has ANALYSIS video generation limit");
        }
    }

    public async Task DashboardResponseStatisticQueryTriggerAsync(DashboardResponseStatisticQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "DASHBOARD_RESPONSE_STATISTIC_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        var clients = await _clientRepository.GetListAsync(new ListQueryOptions<Client> { Filter = x => !x.IsBlocked });
        if (clients is { Count: > 0 })
        {
            foreach (var client in clients)
            {
                await EventBus.PublishAsync(eventMessage: new DashboardResponseStatisticQueryEto(AppClientId: client.Id), correlationId: correlationId);
            }
        }
        else
        {
            _logger.LogWarning("There is no client which is not blocked");
        }
    }

    public async Task TrendVideoGenerationQueryTriggerAsync(TrendVideoGenerationQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "TREND_VIDEO_GENERATION_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        var clients = await _clientRepository.GetListAsync(new ListQueryOptions<Client> { Filter = x => !x.IsBlocked && x.DailyTrendVideoGenerationLimit > 0 });
        if (clients is { Count: > 0 })
        {
            foreach (var client in clients)
            {
                await EventBus.PublishAsync(eventMessage: new TrendVideoGenerationQueryEto(AppClientId: client.Id), correlationId: correlationId);
            }
        }
        else
        {
            _logger.LogWarning("There is no client which has TREND video generation limit");
        }
    }

    public async Task TestQueryTriggerAsync(TestQueryTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "TEST_QUERY",
            correlationId: correlationId,
            exception: null
        ));

        await EventBus.PublishAsync(eventMessage: new TestQueryRequestedEto(AppClientId: Guid.CreateVersion7()), correlationId: correlationId);
    }
}