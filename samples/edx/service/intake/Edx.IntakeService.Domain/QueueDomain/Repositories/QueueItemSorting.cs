using Edx.IntakeService.Domain.QueueDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.IntakeService.Domain.QueueDomain.Repositories;

/// <summary>The fields a list of the queue may be sorted by (the names a client sends) and the sorting when it sends none: the newest first.</summary>
public static class QueueItemSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<QueueItem> Fields = new SortFields<QueueItem>()
        .Add("createdAt", item => item.CreatedAt)
        .Add("invoiceNumber", item => item.InvoiceNumber)
        .Add("status", item => item.Status)
        .Add("sizeBytes", item => item.SizeBytes)
        .Add("statusChangedAt", item => item.StatusChangedAt);
}
