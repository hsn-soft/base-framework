using Hhs.FeedRService.Application.Contracts.Events;
using Hhs.FeedRService.Application.Contracts.Events.Reporting;
using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Hhs.FeedRService.Application.Contracts.DashboardDomain;

namespace Hhs.FeedRService.Application.Services;

public sealed class JobAppService : ApplicationServiceBase, IJobAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IGoogleReportService _reportGoogleService;
    private readonly IReportPersistenceService _reportPersistenceService;

    public JobAppService(
        IServiceProvider provider,
        IGoogleReportService reportService
    ) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
        _reportGoogleService = reportService;
    }

    public async Task GenerateSummaryReportTriggerAsync(GenerateSummaryReportTriggerDto input,
        string correlationId = null)
    {
        string str = input.ClientId==Guid.Empty?"All":input.ClientId.ToString();

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered for ClientId:{str}",
            reference: new { RefContentId = input.JobName },
            facility: "GENERATE_SUMMARY_REPORT",
            correlationId: correlationId,
            exception: null
        ));

        await EventBus.PublishAsync(eventMessage: new GenerateSummaryReportRequestedEto(input.ClientId, input.DateRange));
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

        await EventBus.PublishAsync(eventMessage: new TestQueryCompletedEto(AppClientId: input.AppClientId), correlationId: correlationId);
    }

    public async Task DerivePendingReportsAsync(DerivePendingReportsTriggerDto input,
        string correlationId = null)
    {

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered with MaxCount:{input.MaxCount}",
            reference: new { RefContentId = input.JobName },
            facility: "DERIVE_PENDING_REPORTS",
            correlationId: correlationId,
            exception: null
        ));

        await EventBus.PublishAsync(eventMessage: new DerivePendingReportsRequestedEto(input.MaxCount));
    }

}