using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.IntakeService.Domain.StoredFileDomain.Repositories;

/// <summary>The fields a list of files may be sorted by (the names a client sends, <c>"sizeBytes desc, fileName"</c>) and the sorting when it sends none: the newest first.</summary>
public static class StoredFileSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<StoredFile> Fields = new SortFields<StoredFile>()
        .Add("createdAt", file => file.CreatedAt)
        .Add("fileName", file => file.FileName)
        .Add("sizeBytes", file => file.SizeBytes)
        .Add("kind", file => file.Kind)
        .Add("channel", file => file.Channel);
}
