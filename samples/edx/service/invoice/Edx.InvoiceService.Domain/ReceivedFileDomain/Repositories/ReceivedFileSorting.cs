using Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.InvoiceService.Domain.ReceivedFileDomain.Repositories;

/// <summary>The fields a list of received files may be sorted by and the sorting when a client sends none: the newest first.</summary>
public static class ReceivedFileSorting
{
    public const string Default = "receivedAt desc";

    public static readonly SortFields<ReceivedFile> Fields = new SortFields<ReceivedFile>()
        .Add("receivedAt", file => file.ReceivedAt)
        .Add("fileName", file => file.FileName)
        .Add("sizeBytes", file => file.SizeBytes)
        .Add("kind", file => file.Kind);
}
