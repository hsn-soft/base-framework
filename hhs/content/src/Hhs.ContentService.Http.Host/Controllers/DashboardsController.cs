using Hhs.ContentService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.ContentService.Application.Contracts.DashboardDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using Hhs.Shared.Contracts.Cache.ServicePermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/dashboards")]
public sealed class DashboardsController : BaseServiceController
{
    private readonly IDashboardAppService _dashboardAppService;

    public DashboardsController(IServiceProvider provider, IDashboardAppService dashboardAppService) : base(provider)
    {
        _dashboardAppService = dashboardAppService;
    }

    [Authorize(ContentServicePermissions.Dashboards.ResponseStatisticView)]
    [HttpGet("daily-responses-totals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<DailyResponsesTotalsDto> DailyResponsesTotalsAsync() => await _dashboardAppService.GetDailyResponsesTotalsAsync();

    [Authorize(ContentServicePermissions.Dashboards.ResponseStatisticView)]
    [HttpGet("weekly-responses-analysis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<WeeklyResponsesAnalysisDto> WeeklyResponsesAnalysisAsync() => await _dashboardAppService.GetWeeklyResponsesAnalysisAsync();

    [Authorize(ContentServicePermissions.Dashboards.ResponseStatisticView)]
    [HttpGet("monthly-responses-totals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<MonthlyResponsesTotalsDto> MonthlyResponsesTotalsAsync() => await _dashboardAppService.GetMonthlyResponsesTotalsAsync();

    [Authorize(ContentServicePermissions.Dashboards.ResponseStatisticView)]
    [HttpGet("monthly-responses-analysis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<MonthlyResponsesAnalysisDto> MonthlyResponsesAnalysisAsync() => await _dashboardAppService.GetMonthlyResponsesAnalysisAsync();
}