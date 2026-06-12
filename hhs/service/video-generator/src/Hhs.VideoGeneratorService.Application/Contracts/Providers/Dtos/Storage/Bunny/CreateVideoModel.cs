using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;

public class BunnyCdnVideoCreateReq
{
    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("collectionId")]
    public string? CollectionId { get; set; }

    [JsonProperty("thumbnailTime")]
    public string? thumbnailTime { get; set; }
}

public class BunnyCdnVideoCreateRes
{
    [JsonProperty("videoLibraryId")]
    public string? VideoLibraryId { get; set; }

    [JsonProperty("guid")]
    public string? Guid { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("isPublic")]
    public bool? IsPublic { get; set; }

    [JsonProperty("length")]
    public int? length { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }
}