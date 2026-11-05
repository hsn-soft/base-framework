using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Common;

/// <summary>
/// A step of the work of a service failed. <paramref name="Milestone"/> is the step, <paramref name="Retryable"/> says whether the service will try again (<see langword="false"/>: the work ended, and the content service marks the content as failed).
/// Published by the text normalizer and the video generator.
/// </summary>
public sealed record MilestoneFailedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, string Service, string Milestone, string Reason, bool Retryable) : IIntegrationEventMessage;
