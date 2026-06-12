using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AppContentVisit : Entity<Guid>, IScopeSubscription
{
    [NotNull] public string ScopeKey { get; private set; }

    public Guid AppContentId { get; private set; }

    public DateTime VisitTime { get; private set; }

    [NotNull] public string VisitResponse { get; private set; }


    private AppContentVisit()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        VisitResponse = string.Empty;
    }

    internal AppContentVisit(Guid customerId, ProductTypes productType, Guid appContentId, [NotNull] string visitResponse)
        : this(Guid.CreateVersion7(), customerId, productType, appContentId, visitResponse)
    {
    }

    internal AppContentVisit(Guid id, Guid customerId, ProductTypes productType, Guid appContentId, [NotNull] string visitResponse) : this()
    {
        Id = id;

        SetScopeKey(customerId, productType);
        AppContentId = appContentId;

        VisitTime = DateTime.UtcNow;
        SetVisitResponse(visitResponse);
    }

    private void SetScopeKey(Guid customerId, ProductTypes productType)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, productType),
            $"{nameof(AppContent)}:{nameof(ScopeKey)}",
            AppContentVisitConsts.ScopeKeyMaxLength
        );

    internal void SetVisitResponse(string visitResponse)
    {
        string checkVisitResponse = LocalizedModelValidator.NotNullOrWhiteSpace(visitResponse, $"{nameof(AppContent)}:{nameof(VisitResponse)}", AppContentVisitConsts.VisitResponseMaxLength);
        switch (checkVisitResponse)
        {
            case AppContentPublicStatus.READY:
            case AppContentPublicStatus.CREATED:
            case AppContentPublicStatus.NO_ANALYSIS_VIDEO:
            case AppContentPublicStatus.SKIPPED_PATH:
                {
                    break;
                }
            default: throw new ArgumentException($"{nameof(VisitResponse)} is invalid", nameof(visitResponse));
        }

        VisitResponse = StringHelper.Minimize(checkVisitResponse);
    }
}