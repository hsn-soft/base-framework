using HsnSoft.Base.Application.Dtos;

namespace Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos;

/// <summary>One thing that happened to an envelope.</summary>
public sealed class EnvelopeEventDto
{
    public DateTimeOffset At { get; init; }

    public int Code { get; init; }

    public string Text { get; init; } = string.Empty;

    /// <summary><c>Local</c>, <c>Send</c>, <c>Center</c>, <c>Poll</c> or <c>PostBox</c>.</summary>
    public string Source { get; init; } = string.Empty;

    public string? Note { get; init; }
}

/// <summary>An envelope the service handed to the center (an invoice of the tenant, or the answer of its post box about one that came in), and where it is in its road.</summary>
public sealed class OutboundEnvelopeDto : EntityDto<Guid>
{
    /// <summary>The uuid of the envelope (what the center knows it by).</summary>
    public string EnvelopeId { get; init; } = string.Empty;

    /// <summary><c>Invoice</c> or <c>SystemResponse</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary><c>ToSend</c>, <c>Sent</c>, <c>Delivered</c>, <c>Refused</c> or <c>Failed</c>.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>What the center last said of it: its code and its text.</summary>
    public int StatusCode { get; init; }

    public string StatusDescription { get; init; } = string.Empty;

    public string? FailureStage { get; init; }

    public string? FailureReason { get; init; }

    public string SenderAlias { get; init; } = string.Empty;

    public string ReceiverAlias { get; init; } = string.Empty;

    public string ReceiverRegisterNumber { get; init; } = string.Empty;

    public string ReceiverTitle { get; init; } = string.Empty;

    public Guid? InvoiceId { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? ReferenceEnvelopeId { get; init; }

    public int SendCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset StatusChangedAt { get; init; }

    public Guid TenantId { get; init; }

    public IReadOnlyList<EnvelopeEventDto> History { get; init; } = [];
}

/// <summary>An envelope the center delivered to the post box of the tenant (an invoice for it, or a system response about one of its envelopes), and what the post box made of it.</summary>
public sealed class InboundEnvelopeDto : EntityDto<Guid>
{
    public string EnvelopeId { get; init; } = string.Empty;

    /// <summary><c>Queued</c>, <c>Accepted</c>, <c>Refused</c>, <c>Applied</c> or <c>Ignored</c>.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>What the post box says of it: its code (1200 taken, or the check it failed) and its text.</summary>
    public int StatusCode { get; init; }

    public string StatusDescription { get; init; } = string.Empty;

    /// <summary><c>SENDERENVELOPE</c> (an invoice) or <c>SYSTEMENVELOPE</c> (a system response).</summary>
    public string? DocumentType { get; init; }

    public string? SenderAlias { get; init; }

    public string? SenderRegisterNumber { get; init; }

    public string? SenderTitle { get; init; }

    public string? ReceiverAlias { get; init; }

    public Guid? InvoiceUuid { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? ReferenceEnvelopeId { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; init; }

    public Guid TenantId { get; init; }

    /// <summary>What the center says of the envelope (1220, 1300...), when it was asked.</summary>
    public int? CenterStatusCode { get; init; }

    public string? CenterStatusDescription { get; init; }

    public DateTimeOffset? CenterStatusAt { get; init; }

    public IReadOnlyList<EnvelopeEventDto> History { get; init; } = [];
}
