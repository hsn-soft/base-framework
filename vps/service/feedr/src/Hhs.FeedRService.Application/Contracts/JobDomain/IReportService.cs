using Google.Ads.AdManager.V1;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;
using HsnSoft.Base.EventBus;

namespace Hhs.FeedRService.Application.Contracts.JobDomain;

public interface IGoogleReportService : IEventApplicationService
{
    Task<string> CreateReportAsync(CreateReportRequest request);
    CreateReportRequest CreateReportRequest(CreateReportRequestDto input);
    Task<GenerateSummaryReportRes> GenerateSummaryReportAsync(Guid appClientId, string dateRange);
    Task<GenerateSummaryReportRes> GenerateSummaryReportForAllAsync(string dateRange);
    Task<string> RetrieveInventoriesAsync(Guid appClientId);
}