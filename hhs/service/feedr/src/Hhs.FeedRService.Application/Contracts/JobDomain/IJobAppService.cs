using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;
using JetBrains.Annotations;

namespace Hhs.FeedRService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task TestQueryTriggerAsync(TestQueryTriggerDto input, [CanBeNull] string correlationId = null);
    Task GenerateSummaryReportTriggerAsync(GenerateSummaryReportTriggerDto input, string getJobCorrelationId);
    Task DerivePendingReportsAsync(DerivePendingReportsTriggerDto input, [CanBeNull] string correlationId = null);
}