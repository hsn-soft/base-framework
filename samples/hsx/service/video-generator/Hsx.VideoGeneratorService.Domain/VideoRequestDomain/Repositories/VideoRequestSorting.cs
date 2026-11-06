using HsnSoft.Base.Domain.Sorting;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Entities;

namespace Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Repositories;

/// <summary>The fields a list of video requests may be sorted by and the sorting when it sends none.</summary>
public static class VideoRequestSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<VideoRequest> Fields = new SortFields<VideoRequest>()
        .Add("createdAt", entity => entity.CreatedAt);
}
