using FluentAssertions;
using Hhs.FeedRService.Application.ReportingDomain;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

public class GoogleAdManagerResponseParserTests
{
    private static string LoadTestData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "ExampleGoogleAdmResponse.json");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Parse_WithExampleResponse_ReturnsExpectedRowCount()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        rows.Should().NotBeEmpty();
        // The example file has multiple NDJSON lines
        rows.Count.Should().BeGreaterThan(5);
    }

    [Fact]
    public void Parse_ExtractsDimensionValues_Correctly()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        var first = rows[0];
        first.AdUnitIdTopLevel.Should().Be("23211338768");
        first.AdUnitId.Should().NotBeNullOrEmpty();
        first.DemandChannel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_ExtractsMetricValues_Correctly()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        var first = rows[0];
        // First row: CodeServedCount=20, Impressions=3, AverageEcpm=233.33..., Revenue=0.7, ActiveView=3
        first.CodeServedCount.Should().Be(20);
        first.Impressions.Should().Be(3);
        first.AverageEcpm.Should().BeApproximately(233.333, 0.01);
        first.Revenue.Should().BeApproximately(0.7, 0.001);
        first.ActiveViewEligibleImpressions.Should().Be(3);
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsEmptyList()
    {
        GoogleAdManagerResponseParser.Parse("").Should().BeEmpty();
        GoogleAdManagerResponseParser.Parse(null).Should().BeEmpty();
        GoogleAdManagerResponseParser.Parse("   ").Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithMalformedLine_SkipsItAndParsesRest()
    {
        var validLine = "{ \"dimensionValues\": [ { \"intValue\": \"111\" }, { \"intValue\": \"222\" }, { \"stringValue\": \"CH\" }, { \"stringValue\": \"Sub\" }, { \"intValue\": \"0\" }, { \"stringValue\": \"\" } ], \"metricValueGroups\": [ { \"primaryValues\": [ { \"intValue\": \"10\" }, { \"intValue\": \"5\" }, { \"doubleValue\": 1.5 }, { \"doubleValue\": 0.5 }, { \"intValue\": \"5\" } ] } ] }";
        var input = "NOT_JSON\n" + validLine + "\nALSO_BAD";

        var rows = GoogleAdManagerResponseParser.Parse(input);
        rows.Should().HaveCount(1);
        rows[0].AdUnitIdTopLevel.Should().Be("111");
    }

    [Fact]
    public void Parse_GroupsByAdUnitId_MultipleAdUnitsPresent()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        var distinctAdUnits = rows.Select(r => r.AdUnitId).Distinct().ToList();
        distinctAdUnits.Count.Should().BeGreaterThan(1,
            "the example response should contain rows for multiple AdUnitIds (different clients)");
    }

    [Fact]
    public void Parse_AllRows_HaveNonNegativeMetrics()
    {
        var ndjson = LoadTestData();
        var rows = GoogleAdManagerResponseParser.Parse(ndjson);

        foreach (var row in rows)
        {
            row.CodeServedCount.Should().BeGreaterThanOrEqualTo(0);
            row.Impressions.Should().BeGreaterThanOrEqualTo(0);
            row.Revenue.Should().BeGreaterThanOrEqualTo(0);
            row.ActiveViewEligibleImpressions.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
