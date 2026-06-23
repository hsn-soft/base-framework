using FluentAssertions;
using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Enums;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

public class RawGoogleAdManagerResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var reportDate = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        var rows = new List<GoogleAdManagerReportRow>
        {
            new() { AdUnitIdTopLevel = "top1", AdUnitId = "unit1", Impressions = 100 }
        };

        var entity = new RawGoogleAdManagerResponse(
            id: id,
            tenantId: tenantId,
            requestId: "req-123",
            jobName: "GenerateSummaryReport",
            network: "21852615636",
            adUnitIdTopLevel: "23211338768",
            adUnitId: "21863243186",
            reportDate: reportDate,
            rawResponse: "some raw",
            rows: rows);

        entity.Id.Should().Be(id);
        entity.TenantId.Should().Be(tenantId);
        entity.RequestId.Should().Be("req-123");
        entity.JobName.Should().Be("GenerateSummaryReport");
        entity.Network.Should().Be("21852615636");
        entity.AdUnitIdTopLevel.Should().Be("23211338768");
        entity.AdUnitId.Should().Be("21863243186");
        entity.ReportDate.Should().Be(reportDate.Date);
        entity.RawResponse.Should().Be("some raw");
        entity.Rows.Should().HaveCount(1);
        entity.ProcessedStatus.Should().Be(DerivationStatus.Pending);
    }

    [Fact]
    public void MarkProcessing_ChangesStatus()
    {
        var entity = CreateEntity();
        entity.ProcessedStatus.Should().Be(DerivationStatus.Pending);

        entity.MarkProcessing();
        entity.ProcessedStatus.Should().Be(DerivationStatus.Processing);
    }

    [Fact]
    public void MarkCompleted_SetsStatusAndTimestamp()
    {
        var entity = CreateEntity();
        entity.MarkCompleted();

        entity.ProcessedStatus.Should().Be(DerivationStatus.Completed);
        entity.ProcessedAt.Should().NotBeNull();
        entity.ProcessingError.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndError()
    {
        var entity = CreateEntity();
        entity.MarkFailed("Something broke");

        entity.ProcessedStatus.Should().Be(DerivationStatus.Failed);
        entity.ProcessedAt.Should().NotBeNull();
        entity.ProcessingError.Should().Be("Something broke");
    }

    [Fact]
    public void Constructor_HandlesNullStrings_Gracefully()
    {
        var entity = new RawGoogleAdManagerResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            requestId: null,
            jobName: null,
            network: null,
            adUnitIdTopLevel: null,
            adUnitId: null,
            reportDate: DateTime.UtcNow,
            rawResponse: null,
            rows: null);

        entity.RequestId.Should().Be(string.Empty);
        entity.JobName.Should().Be(string.Empty);
        entity.Network.Should().Be(string.Empty);
        entity.AdUnitIdTopLevel.Should().Be(string.Empty);
        entity.AdUnitId.Should().Be(string.Empty);
        entity.RawResponse.Should().Be(string.Empty);
        entity.Rows.Should().BeEmpty();
    }

    private static RawGoogleAdManagerResponse CreateEntity()
    {
        return new RawGoogleAdManagerResponse(
            id: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            requestId: "req-1",
            jobName: "Job",
            network: "net1",
            adUnitIdTopLevel: "top1",
            adUnitId: "unit1",
            reportDate: DateTime.UtcNow,
            rawResponse: "{}",
            rows: new List<GoogleAdManagerReportRow>());
    }
}
