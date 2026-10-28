using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;

/// <summary>The fields the lists of envelopes may be sorted by, and the sorting when a client sends none: the latest first.</summary>
public static class EnvelopeSorting
{
    public const string OutboundDefault = "createdAt desc";

    public const string InboundDefault = "receivedAt desc";

    public static readonly SortFields<OutboundEnvelope> Outbound = new SortFields<OutboundEnvelope>()
        .Add("createdAt", envelope => envelope.CreatedAt)
        .Add("statusChangedAt", envelope => envelope.StatusChangedAt)
        .Add("state", envelope => envelope.State)
        .Add("statusCode", envelope => envelope.StatusCode);

    public static readonly SortFields<InboundEnvelope> Inbound = new SortFields<InboundEnvelope>()
        .Add("receivedAt", envelope => envelope.ReceivedAt)
        .Add("state", envelope => envelope.State)
        .Add("statusCode", envelope => envelope.StatusCode);
}
