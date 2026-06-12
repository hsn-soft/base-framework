using FluentAssertions;
using Hhs.FeedRService.Domain.ReportingDomain.Entities;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

public class DailyReportResponseAggregationTests
{
    [Fact]
    public void MergeFrom_AddsTotalsCorrectly()
    {
        var report = new DashboardResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            adUnitClientId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            reportDate: DateTime.UtcNow.Date,
            demandChannel: "Web",
            demandSubchannelName: "Standard",
            orderId: "Order123",
            orderName: "Sample Order",
            codeServedCount: 100,
            impressions: 1000,
            revenue: 50.0,
            activeViewEligibleImpressions: 800,
            averageEcpm: 10.0,
            sourceRowCount: 5,
            sourceMongoDbId: Guid.NewGuid());

        var newSourceId = Guid.NewGuid();
        report.MergeFrom(
            codeServedCount: 200,
            impressions: 2000,
            revenue: 100.0,
            activeViewEligibleImpressions: 1500,
            averageEcpm: 20.0,
            sourceRowCount: 10,
            sourceMongoDbId: newSourceId);

        report.CodeServedCount.Should().Be(300);
        report.Impressions.Should().Be(3000);
        report.Revenue.Should().BeApproximately(150.0, 0.001);
        report.ActiveViewEligibleImpressions.Should().Be(2300);
        report.SourceRowCount.Should().Be(15);
        report.SourceMongoDbId.Should().Be(newSourceId);
    }

    [Fact]
    public void MergeFrom_CalculatesWeightedAverageEcpm()
    {
        var report = new DashboardResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            adUnitClientId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            reportDate: DateTime.UtcNow.Date,
            demandChannel: "Web",
            demandSubchannelName: "Standard",
            orderId: "Order123",
            orderName: "Sample Order",
            codeServedCount: 10,
            impressions: 1000,
            revenue: 10.0,
            activeViewEligibleImpressions: 900,
            averageEcpm: 10.0,   // weighted contribution = 10 * 1000 = 10000
            sourceRowCount: 1,
            sourceMongoDbId: Guid.NewGuid());

        report.MergeFrom(
            codeServedCount: 20,
            impressions: 3000,
            revenue: 60.0,
            activeViewEligibleImpressions: 2700,
            averageEcpm: 20.0,   // weighted contribution = 20 * 3000 = 60000
            sourceRowCount: 3,
            sourceMongoDbId: Guid.NewGuid());

        // Expected: (10000 + 60000) / (1000 + 3000) = 70000 / 4000 = 17.5
        report.AverageEcpm.Should().BeApproximately(17.5, 0.001);
    }

    [Fact]
    public void MergeFrom_WithZeroImpressions_DoesNotDivideByZero()
    {
        var report = new DashboardResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            adUnitClientId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            reportDate: DateTime.UtcNow.Date,
            demandChannel: "Web",
            demandSubchannelName: "Standard",
            orderId: "Order123",
            orderName: "Sample Order",
            codeServedCount: 0,
            impressions: 0,
            revenue: 0,
            activeViewEligibleImpressions: 0,
            averageEcpm: 0,
            sourceRowCount: 0,
            sourceMongoDbId: Guid.NewGuid());

        report.MergeFrom(0, 0, 0, 0, 0, 0, Guid.NewGuid());

        report.AverageEcpm.Should().Be(0);
        report.Impressions.Should().Be(0);
    }

    [Fact]
    public void Constructor_SetsReportDateToDateOnly()
    {
        var dateWithTime = new DateTime(2026, 4, 21, 14, 30, 0, DateTimeKind.Utc);
        var report = new DashboardResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            adUnitClientId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            reportDate: dateWithTime,
            demandChannel: "Web",
            demandSubchannelName: "Standard",
            orderId: "Order123",
            orderName: "Sample Order",
            codeServedCount: 1,
            impressions: 1,
            revenue: 1,
            activeViewEligibleImpressions: 1,
            averageEcpm: 1,
            sourceRowCount: 1,
            sourceMongoDbId: Guid.NewGuid());

        report.ReportDate.Should().Be(dateWithTime.Date);
        report.ReportDate.TimeOfDay.Should().Be(TimeSpan.Zero);
    }
}
