using Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.DashboardDomain.Interfaces;

public interface IDashboardAppService : IEventApplicationService
{
    Task<DailyResponsesTotalsDto> GetDailyResponsesTotalsAsync(CancellationToken cancellationToken = default);
    Task<WeeklyResponsesAnalysisDto> GetWeeklyResponsesAnalysisAsync(CancellationToken cancellationToken = default);
    Task<MonthlyResponsesTotalsDto> GetMonthlyResponsesTotalsAsync(CancellationToken cancellationToken = default);
    Task<MonthlyResponsesAnalysisDto> GetMonthlyResponsesAnalysisAsync(CancellationToken cancellationToken = default);

    Task DashboardResponseStatisticQueryAsync(DashboardResponseStatisticQueryEto input, [CanBeNull] string correlationId = null);
}