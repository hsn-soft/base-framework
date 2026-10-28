using System.Linq.Expressions;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos.Filters;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Services;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Exceptions;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>The reads of the envelopes of a tenant (the persistence narrows them to the caller's tenant).</summary>
public sealed class EnvelopeAppService(IOutboundEnvelopeRepository outbound, IInboundEnvelopeRepository inbound, IServiceProvider services) : ApplicationService(services), IEnvelopeAppService
{
    public async Task<OutboundEnvelopeDto> GetOutboundAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await outbound.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw EnvelopeNotFound.For(id));

    public async Task<OutboundEnvelopeDto> GetOutboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        => ToDto(await outbound.FindByEnvelopeIdAsync(envelopeId.ToString(), cancellationToken).ConfigureAwait(false) ?? throw EnvelopeNotFound.For(envelopeId));

    public async Task<InboundEnvelopeDto> GetInboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        => ToDto(await inbound.FindByEnvelopeIdAsync(envelopeId.ToString(), cancellationToken).ConfigureAwait(false) ?? throw EnvelopeNotFound.For(envelopeId));

    public async Task<PagedDataResultDto<OutboundEnvelopeDto>> GetOutboundPagedListAsync(GetOutboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        OutboundState? state = Parse<OutboundState>(pagedInput.State, nameof(pagedInput.State));
        OutboundKind? kind = Parse<OutboundKind>(pagedInput.Kind, nameof(pagedInput.Kind));
        string? number = string.IsNullOrWhiteSpace(pagedInput.InvoiceNumber) ? null : pagedInput.InvoiceNumber.Trim();
        Expression<Func<OutboundEnvelope, bool>> filter = new FilterBuilder<OutboundEnvelope>()
            .And(state is null ? null : envelope => envelope.State == state.Value)
            .And(kind is null ? null : envelope => envelope.Kind == kind.Value)
            .And(number is null ? null : envelope => envelope.InvoiceNumber!.Contains(number))
            .Build();
        PagedQueryResult<OutboundEnvelope> page = await outbound.GetPagedListAsync(pagedInput.ToPagedOptions(filter, EnvelopeSorting.Outbound, EnvelopeSorting.OutboundDefault), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<InboundEnvelopeDto> GetInboundAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await inbound.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw EnvelopeNotFound.For(id));

    public async Task<PagedDataResultDto<InboundEnvelopeDto>> GetInboundPagedListAsync(GetInboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        InboundState? state = Parse<InboundState>(pagedInput.State, nameof(pagedInput.State));
        string? documentType = string.IsNullOrWhiteSpace(pagedInput.DocumentType) ? null : pagedInput.DocumentType.Trim();
        Expression<Func<InboundEnvelope, bool>> filter = new FilterBuilder<InboundEnvelope>()
            .And(state is null ? null : envelope => envelope.State == state.Value)
            .And(documentType is null ? null : envelope => envelope.DocumentType == documentType)
            .Build();
        PagedQueryResult<InboundEnvelope> page = await inbound.GetPagedListAsync(pagedInput.ToPagedOptions(filter, EnvelopeSorting.Inbound, EnvelopeSorting.InboundDefault), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    private static T? Parse<T>(string? text, string name)
        where T : struct, Enum
        => string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.TryParse(text.Trim(), ignoreCase: true, out T parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("filter_value_invalid", $"'{text}' is not a value of {name}.").WithKey("filter_value_invalid", name, text);

    private static OutboundEnvelopeDto ToDto(OutboundEnvelope envelope) => new()
    {
        Id = envelope.Id,
        EnvelopeId = envelope.EnvelopeId,
        Kind = envelope.Kind.ToString(),
        State = envelope.State.ToString(),
        StatusCode = envelope.StatusCode,
        StatusDescription = envelope.StatusDescription,
        FailureStage = envelope.FailureStage,
        FailureReason = envelope.FailureReason,
        SenderAlias = envelope.SenderAlias,
        ReceiverAlias = envelope.ReceiverAlias,
        ReceiverRegisterNumber = envelope.ReceiverRegisterNumber,
        ReceiverTitle = envelope.ReceiverTitle,
        InvoiceId = envelope.InvoiceId,
        InvoiceNumber = envelope.InvoiceNumber,
        ReferenceEnvelopeId = envelope.ReferenceEnvelopeId,
        SendCount = envelope.SendCount,
        CreatedAt = envelope.CreatedAt,
        StatusChangedAt = envelope.StatusChangedAt,
        TenantId = envelope.TenantId,
        History = ToDtos(envelope.History),
    };

    private static InboundEnvelopeDto ToDto(InboundEnvelope envelope) => new()
    {
        Id = envelope.Id,
        EnvelopeId = envelope.EnvelopeId,
        State = envelope.State.ToString(),
        StatusCode = envelope.StatusCode,
        StatusDescription = envelope.StatusDescription,
        DocumentType = envelope.DocumentType,
        SenderAlias = envelope.SenderAlias,
        SenderRegisterNumber = envelope.SenderRegisterNumber,
        SenderTitle = envelope.SenderTitle,
        ReceiverAlias = envelope.ReceiverAlias,
        InvoiceUuid = envelope.InvoiceUuid,
        InvoiceNumber = envelope.InvoiceNumber,
        ReferenceEnvelopeId = envelope.ReferenceEnvelopeId,
        ReceivedAt = envelope.ReceivedAt,
        ProcessedAt = envelope.ProcessedAt,
        TenantId = envelope.TenantId,
        CenterStatusCode = envelope.CenterStatusCode,
        CenterStatusDescription = envelope.CenterStatusDescription,
        CenterStatusAt = envelope.CenterStatusAt,
        History = ToDtos(envelope.History),
    };

    private static IReadOnlyList<EnvelopeEventDto> ToDtos(IEnumerable<EnvelopeEvent> history)
        => [.. history.Select(entry => new EnvelopeEventDto { At = entry.At, Code = entry.Code, Text = entry.Text, Source = entry.Source, Note = entry.Note })];
}
