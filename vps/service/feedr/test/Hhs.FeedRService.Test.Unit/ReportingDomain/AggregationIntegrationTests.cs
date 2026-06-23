using FluentAssertions;
using Hhs.FeedRService.Application.ReportingDomain;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

/// <summary>
/// Tests the aggregation logic that ReportPersistenceService.AggregateRows uses internally.
/// Because AggregateRows is private, we validate the same math by grouping parsed rows and
/// computing the expected sums and weighted averages manually from the real example response.
/// </summary>
public class AggregationIntegrationTests
{
    private static string LoadTestData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "ExampleGoogleAdmResponse.json");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ParsedRows_GroupedByAdUnit_SumTotalsAreConsistent()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        var grouped = rows
            .Where(r => !string.IsNullOrEmpty(r.AdUnitId))
            .GroupBy(r => r.AdUnitId);

        foreach (var group in grouped)
        {
            var list = group.ToList();

            long totalImpressions = list.Sum(r => r.Impressions);
            long totalCodeServed = list.Sum(r => r.CodeServedCount);
            double totalRevenue = list.Sum(r => r.Revenue);

            totalImpressions.Should().BeGreaterThanOrEqualTo(0);
            totalCodeServed.Should().BeGreaterThanOrEqualTo(0);
            totalRevenue.Should().BeGreaterThanOrEqualTo(0);

            // Weighted average eCPM should be finite
            if (totalImpressions > 0)
            {
                double weightedEcpm = list.Sum(r => r.AverageEcpm * r.Impressions) / totalImpressions;
                weightedEcpm.Should().BeGreaterThanOrEqualTo(0);
                double.IsNaN(weightedEcpm).Should().BeFalse();
                double.IsInfinity(weightedEcpm).Should().BeFalse();
            }
        }
    }

    [Fact]
    public void ParsedRows_AdUnitId23278547678_SumsMatchExpected()
    {
        // Verify against the first AdUnitId in the example file: 23278547678
        // First 3 rows of the example:
        // Row 1: CodeServed=20, Impressions=3,  eCPM=233.333, Revenue=0.7,   ActiveView=3
        // Row 2: CodeServed=48, Impressions=14, eCPM=30,      Revenue=0.42,  ActiveView=14
        // Row 3: CodeServed=4,  Impressions=4,  eCPM=1.1685,  Revenue=0.0047,ActiveView=4
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        var unitRows = rows.Where(r => r.AdUnitId == "23278547678").ToList();
        unitRows.Should().HaveCount(3, "the example has exactly 3 rows for AdUnitId 23278547678");

        long impressions = unitRows.Sum(r => r.Impressions);
        impressions.Should().Be(3 + 14 + 4); // = 21

        long codeServed = unitRows.Sum(r => r.CodeServedCount);
        codeServed.Should().Be(20 + 48 + 4); // = 72

        double revenue = unitRows.Sum(r => r.Revenue);
        revenue.Should().BeApproximately(0.7 + 0.42 + 0.0046740578344857155, 0.001);
    }

    [Fact]
    public void ParsedRows_AllTopLevelValues_AreConsistentlySet()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        // All rows in the example response should share the same AdUnitIdTopLevel: 23211338768
        var topLevels = rows.Select(r => r.AdUnitIdTopLevel).Distinct().ToList();
        topLevels.Should().ContainSingle("the example has one top-level ad unit");
        topLevels[0].Should().Be("23211338768");
    }
}
