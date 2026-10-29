namespace Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;

/// <summary>One step in the life of an invoice: its status changed, or its envelope did.</summary>
public sealed class InvoiceHistoryDto
{
    /// <summary><c>Status</c> or <c>Envelope</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public DateTimeOffset At { get; init; }

    public Guid? By { get; init; }

    public string? Note { get; init; }
}

/// <summary>The answer of the receiver to a commercial invoice (the shape only: the flow is not made yet).</summary>
public sealed class InvoiceResponseDto
{
    /// <summary><c>Accepted</c> or <c>Rejected</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public Guid DocumentUuid { get; init; }

    public DateTimeOffset At { get; init; }

    public string? Note { get; init; }

    public Guid? EnvelopeId { get; init; }

    /// <summary>Where the envelope of the answer is (an answer that went out): <c>Sent</c>, <c>Delivered</c>, <c>Refused</c> or <c>Failed</c>.</summary>
    public string? EnvelopeState { get; init; }

    public int? EnvelopeStatusCode { get; init; }

    public string? EnvelopeStatusText { get; init; }

    public DateTimeOffset? EnvelopeStatusAt { get; init; }
}
