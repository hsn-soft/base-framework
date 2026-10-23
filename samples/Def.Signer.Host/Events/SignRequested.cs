using HsnSoft.Base.EventBus;

namespace Def.Signer.Host.Events;

/// <summary>A caller asked for a document; the <see cref="IntegrationEvent.EventId"/> doubles as the request id the caller polls with.</summary>
public sealed record SignRequested(string Owner, string Title, string Content) : IntegrationEvent;
