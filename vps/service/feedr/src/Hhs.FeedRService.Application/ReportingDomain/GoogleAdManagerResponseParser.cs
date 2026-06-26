using System.Text.Json;
using Hhs.FeedRService.Domain.ReportingDomain.Models;

namespace Hhs.FeedRService.Application.ReportingDomain;

/// <summary>
/// Parses a Google Ad Manager <c>FetchReportResultRows</c> NDJSON response body
/// into strongly-typed <see cref="GoogleAdManagerReportRow"/> records.
///
/// The dimensions and metrics order must match the request built in
/// <c>ReportService.CreateReportRequest</c>:
///   Dimensions: AdUnitIdTopLevel, AdUnitId, DemandChannel, DemandSubchannelName, OrderId, OrderName
///   Metrics:    CodeServedCount, Impressions, AverageEcpm, Revenue, ActiveViewEligibleImpressions
/// </summary>
public static class GoogleAdManagerResponseParser
{
    public static List<GoogleAdManagerReportRow> Parse(string ndjsonBody)
    {
        var rows = new List<GoogleAdManagerReportRow>();
        if (string.IsNullOrWhiteSpace(ndjsonBody)) return rows;

        var lines = ndjsonBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed == "{" || trimmed == "}") continue;

            try
            {
                rows.Add(ParseLine(trimmed));
            }
            catch (JsonException)
            {
                // Skip malformed lines but keep processing the rest
            }
        }

        return rows;
    }

    private static GoogleAdManagerReportRow ParseLine(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var row = new GoogleAdManagerReportRow();

        if (root.TryGetProperty("dimensionValues", out var dims) && dims.ValueKind == JsonValueKind.Array)
        {
            var dimArray = dims.EnumerateArray().ToArray();
            row.AdUnitIdTopLevel = GetStringOrInt(dimArray, 0);
            row.AdUnitId = GetStringOrInt(dimArray, 1);
            row.DemandChannel = GetStringOrInt(dimArray, 2);
            row.DemandSubchannelName = GetStringOrInt(dimArray, 3);
            row.OrderId = GetStringOrInt(dimArray, 4);
            row.OrderName = GetStringOrInt(dimArray, 5);
            row.Date = GetStringOrInt(dimArray, 6);
        }

        if (root.TryGetProperty("metricValueGroups", out var groups) && groups.ValueKind == JsonValueKind.Array)
        {
            var firstGroup = groups.EnumerateArray().FirstOrDefault();
            if (firstGroup.ValueKind == JsonValueKind.Object &&
                firstGroup.TryGetProperty("primaryValues", out var primary) &&
                primary.ValueKind == JsonValueKind.Array)
            {
                var metrics = primary.EnumerateArray().ToArray();
                row.CodeServedCount = GetLong(metrics, 0);
                row.Impressions = GetLong(metrics, 1);
                row.AverageEcpm = GetDouble(metrics, 2);
                row.Revenue = GetDouble(metrics, 3);
                row.ActiveViewEligibleImpressions = GetLong(metrics, 4);
            }
        }

        return row;
    }

    private static string GetStringOrInt(JsonElement[] arr, int idx)
    {
        if (idx >= arr.Length) return string.Empty;
        var el = arr[idx];
        if (el.ValueKind != JsonValueKind.Object) return string.Empty;

        if (el.TryGetProperty("stringValue", out var s)) return s.GetString() ?? string.Empty;
        if (el.TryGetProperty("intValue", out var i)) return i.GetString() ?? string.Empty;
        if (el.TryGetProperty("doubleValue", out var d)) return d.GetRawText();
        return string.Empty;
    }

    private static long GetLong(JsonElement[] arr, int idx)
    {
        if (idx >= arr.Length) return 0;
        var el = arr[idx];
        if (el.ValueKind != JsonValueKind.Object) return 0;

        if (el.TryGetProperty("intValue", out var i))
        {
            var raw = i.GetString();
            return long.TryParse(raw, out var v) ? v : 0;
        }
        if (el.TryGetProperty("doubleValue", out var d))
        {
            return (long)d.GetDouble();
        }
        return 0;
    }

    private static double GetDouble(JsonElement[] arr, int idx)
    {
        if (idx >= arr.Length) return 0d;
        var el = arr[idx];
        if (el.ValueKind != JsonValueKind.Object) return 0d;

        if (el.TryGetProperty("doubleValue", out var d)) return d.GetDouble();
        if (el.TryGetProperty("intValue", out var i))
        {
            var raw = i.GetString();
            return double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0d;
        }
        return 0d;
    }
}
