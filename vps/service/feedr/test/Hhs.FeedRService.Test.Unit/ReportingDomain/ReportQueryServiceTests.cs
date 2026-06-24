using FluentAssertions;
using Hhs.FeedRService.Application.Services;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories.PostgreSQL;
using AutoMapper;
using Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

public class ReportQueryServiceTests
{
    private readonly IDashboardRepository _dailyRepo;
    private readonly ReportQueryService _sut;

    public ReportQueryServiceTests()
    {
        _dailyRepo = Substitute.For<IDashboardRepository>();

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMapper>());
        services.AddSingleton(Substitute.For<IEventBus>());
        services.AddSingleton(typeof(IStringLocalizer<>), typeof(NullStringLocalizer<>));
        services.AddSingleton(Substitute.For<IStringLocalizerFactory>());
        var provider = services.BuildServiceProvider();

        _sut = new ReportQueryService(provider, _dailyRepo);
    }

    private class NullStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();
    }

    [Fact]
    public async Task GetByAdUnitAndDateRangeAsync_AggregatesSumAndAverage()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var adUnitClientId = Guid.NewGuid();
        var adUnitClient = new AdUnitClient(adUnitClientId, tenantId, Guid.NewGuid(), "UNIT1", clientId, "TestClient");
        var date1 = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        var records = new List<DashboardResponse>
        {
            CreateReport(adUnitClientId, clientId, date1, codeServed: 100, impressions: 1000, revenue: 50, activeView: 800, ecpm: 10, rowCount: 5, adUnitClient: adUnitClient),
            CreateReport(adUnitClientId, clientId, date2, codeServed: 200, impressions: 3000, revenue: 150, activeView: 2400, ecpm: 20, rowCount: 10, adUnitClient: adUnitClient),
        };

        _dailyRepo.GetByAdUnitAndDateRangeAsync("UNIT1", date1, date2, Arg.Any<CancellationToken>())
            .Returns(records);

        var result = await _sut.GetByAdUnitAndDateRangeAsync(new GetReportByAdUnitRangeDto
        {
            AdUnitId = "UNIT1",
            StartDate = date1,
            EndDate = date2
        });

        result.TotalCodeServedCount.Should().Be(300);
        result.TotalImpressions.Should().Be(4000);
        result.TotalRevenue.Should().BeApproximately(200, 0.001);
        result.TotalActiveViewEligibleImpressions.Should().Be(3200);
        // Weighted average: (10*1000 + 20*3000) / 4000 = 70000/4000 = 17.5
        result.AverageEcpm.Should().BeApproximately(17.5, 0.001);
        result.RecordCount.Should().Be(2);
        result.DailyBreakdown.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByClientAndDateRangeAsync_ReturnsCorrectAggregation()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var adUnitClientId = Guid.NewGuid();
        var adUnitClient = new AdUnitClient(adUnitClientId, tenantId, Guid.NewGuid(), "UNIT2", clientId, "Client2");
        var date = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        var records = new List<DashboardResponse>
        {
            CreateReport(adUnitClientId, clientId, date, codeServed: 500, impressions: 5000, revenue: 100, activeView: 4000, ecpm: 15, rowCount: 20, adUnitClient: adUnitClient),
        };

        _dailyRepo.GetByClientAndDateRangeAsync(clientId, date, date, Arg.Any<CancellationToken>())
            .Returns(records);

        var result = await _sut.GetByClientAndDateRangeAsync(new GetReportByClientRangeDto
        {
            ClientId = clientId,
            StartDate = date,
            EndDate = date
        });

        result.TotalImpressions.Should().Be(5000);
        result.AverageEcpm.Should().BeApproximately(15, 0.001);
        result.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetByAdUnitAndDateRangeAsync_WithNoRecords_ReturnsZeros()
    {
        var date = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);
        _dailyRepo.GetByAdUnitAndDateRangeAsync("NONE", date, date, Arg.Any<CancellationToken>())
            .Returns(new List<DashboardResponse>());

        var result = await _sut.GetByAdUnitAndDateRangeAsync(new GetReportByAdUnitRangeDto
        {
            AdUnitId = "NONE",
            StartDate = date,
            EndDate = date
        });

        result.TotalImpressions.Should().Be(0);
        result.AverageEcpm.Should().Be(0);
        result.RecordCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByAdUnitAndDateRangeAsync_InvalidRange_Throws()
    {
        var start = new DateTime(2026, 4, 21);
        var end = new DateTime(2026, 4, 20);

        var act = () => _sut.GetByAdUnitAndDateRangeAsync(new GetReportByAdUnitRangeDto
        {
            AdUnitId = "X",
            StartDate = start,
            EndDate = end
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static DashboardResponse CreateReport(
        Guid adUnitClientId, Guid clientId, DateTime date,
        long codeServed, long impressions, double revenue, long activeView,
        double ecpm, int rowCount, AdUnitClient adUnitClient = null)
    {
        var report = new DashboardResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            adUnitClientId: adUnitClientId,
            clientId: clientId,
            reportDate: date,
            demandChannel: "Web",
            demandSubchannelName: "Standard",
            orderId: "Order123",
            orderName: "Sample Order",
            codeServedCount: codeServed,
            impressions: impressions,
            revenue: revenue,
            activeViewEligibleImpressions: activeView,
            averageEcpm: ecpm,
            sourceRowCount: rowCount,
            sourceMongoDbId: Guid.NewGuid());

        // Use reflection to set the navigation property for the test (EF would do this normally)
        if (adUnitClient != null)
        {
            var prop = typeof(DashboardResponse).GetProperty(nameof(DashboardResponse.AdUnitClient));
            prop?.SetValue(report, adUnitClient);
        }

        return report;
    }
}
