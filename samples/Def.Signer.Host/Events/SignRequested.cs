using HsnSoft.Base.Domain.Entities.Events;

namespace Def.Signer.Host.Events;

/// <summary>A caller asked for a document; <paramref name="RequestId"/> is what the caller polls with.</summary>
public sealed record SignRequested(Guid RequestId, string Owner, string Title, string Content) : IIntegrationEventMessage;
