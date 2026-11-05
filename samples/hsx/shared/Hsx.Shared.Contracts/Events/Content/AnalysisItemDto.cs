namespace Hsx.Shared.Contracts.Events.Content;

/// <summary>One page of an analysis, in its place.</summary>
public sealed record AnalysisItemDto(Guid CustomerContentId, int SortOrder, string ContentKey);
