using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;

namespace Edx.InvoiceService.Application.Services;

/// <summary>The history and the answers kept inside an invoice, as the DTOs of the two kinds of invoice send them.</summary>
internal static class InvoiceHistoryMapper
{
    public static IReadOnlyList<InvoiceHistoryDto> ToDtos(IEnumerable<InvoiceHistoryEntry> history)
        => [.. history.Select(entry => new InvoiceHistoryDto { Kind = entry.Kind, Value = entry.Value, At = entry.At, By = entry.By, Note = entry.Note })];

    public static IReadOnlyList<InvoiceResponseDto> ToDtos(IEnumerable<InvoiceResponse> responses)
        => [.. responses.Select(response => new InvoiceResponseDto { Kind = response.Kind.ToString(), DocumentNumber = response.DocumentNumber, DocumentUuid = response.DocumentUuid, At = response.At, Note = response.Note, EnvelopeId = response.EnvelopeId, EnvelopeState = response.EnvelopeState, EnvelopeStatusCode = response.EnvelopeStatusCode, EnvelopeStatusText = response.EnvelopeStatusText, EnvelopeStatusAt = response.EnvelopeStatusAt })];
}
