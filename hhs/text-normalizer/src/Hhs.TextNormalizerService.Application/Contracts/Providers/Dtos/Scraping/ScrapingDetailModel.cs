using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Scraping;

public class ScrapingDetailModel
{
    [JsonProperty("targetText")]
    public string TargetText { get; set; }

    [JsonProperty("filterValue")]
    public string FilterValue { get; set; }
}