using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Content;

/// <summary>A page of a customer's site was seen for the first time and is now a content. Published by the content service; the text normalizer makes its text.</summary>
public sealed record CustomerContentCreatedEto(string ScopeKey, Guid CustomerContentId, string DomainName, string DomainPath) : IIntegrationEventMessage;

/// <summary>An analysis (the video of the most read pages of a day) was made, with its items in their order. Published by the content service; the text normalizer makes the text of each item.</summary>
public sealed record AnalysisContentCreatedEto(string ScopeKey, Guid AnalysisContentId, string DomainName, IReadOnlyList<AnalysisItemEto> Items) : IIntegrationEventMessage;

/// <summary>One page of an analysis, in its place.</summary>
public sealed record AnalysisItemEto(Guid CustomerContentId, int SortOrder, string ContentKey);

/// <summary>The content passed the rules and a video is to be made of it. Published by the content service; the text normalizer hands its text to the video generator.</summary>
public sealed record VideoGenerationApprovedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType) : IIntegrationEventMessage;
