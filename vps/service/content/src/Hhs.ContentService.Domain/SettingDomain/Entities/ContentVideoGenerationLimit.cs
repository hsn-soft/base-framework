using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.SettingDomain.Entities;

public sealed class ContentVideoGenerationLimit : Entity<Guid>, IScopeSubscription
{
    [NotNull] public string ScopeKey { get; private set; }

    public DateTime VideoGenerationDate { get; private set; }

    public VideoGenerationTypes VideoGenerationType { get; private set; }

    [NotNull] public string ContentReferenceIds { get; private set; }

    private ContentVideoGenerationLimit()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        ContentReferenceIds = string.Empty;
    }

    internal ContentVideoGenerationLimit([NotNull] string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds)
        : this(Guid.CreateVersion7(), scopeKey, videoGenerationDate, videoGenerationType, contentReferenceIds)
    {
    }

    internal ContentVideoGenerationLimit(Guid id, [NotNull] string scopeKey,
        DateTime videoGenerationDate,
        VideoGenerationTypes videoGenerationType,
        [NotNull] string contentReferenceIds
    ) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);

        VideoGenerationDate = videoGenerationDate;
        VideoGenerationType = videoGenerationType;
        SetContentReferenceIds(contentReferenceIds);
    }

    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(ContentVideoGenerationLimit)}:{nameof(ScopeKey)}",
            ContentVideoGenerationLimitConsts.ScopeKeyMaxLength
        );

    internal void SetContentReferenceIds(string contentReferenceIds)
    {
        string checkReferences = LocalizedModelValidator.NotNullOrWhiteSpace(contentReferenceIds, $"{nameof(ContentVideoGenerationLimit)}:{nameof(ContentReferenceIds)}", ContentVideoGenerationLimitConsts.ContentReferenceIdsNameMaxLength);
        ContentReferenceIds = StringHelper.Minimize(checkReferences);
    }
}