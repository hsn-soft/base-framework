using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Models;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerContentVisit : Entity<Guid>, IScopeSubscription
{
    [NotNull] public string ScopeKey { get; private set; }

    public Guid CustomerContentId { get; private set; }

    public DateTime VisitTime { get; private set; }

    [NotNull] public string VisitResponse { get; private set; }


    private CustomerContentVisit()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        VisitResponse = string.Empty;
    }

    internal CustomerContentVisit([NotNull] string scopeKey, Guid customerContentId, [NotNull] string visitResponse)
        : this(Guid.CreateVersion7(), scopeKey, customerContentId, visitResponse)
    {
    }

    internal CustomerContentVisit(Guid id, [NotNull] string scopeKey, Guid customerContentId, [NotNull] string visitResponse) : this()
    {
        Id = id;

        SetScopeKey(scopeKey);
        CustomerContentId = customerContentId;

        VisitTime = DateTime.UtcNow;
        SetVisitResponse(visitResponse);
    }

    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            CustomerContentVisitConsts.ScopeKeyMaxLength
        );

    internal void SetVisitResponse(string visitResponse)
    {
        string checkVisitResponse = LocalizedModelValidator.NotNullOrWhiteSpace(visitResponse, $"{nameof(CustomerContent)}:{nameof(VisitResponse)}", CustomerContentVisitConsts.VisitResponseMaxLength);
        switch (checkVisitResponse)
        {
            case PublicContentStatus.READY:
            case PublicContentStatus.CREATED:
            case PublicContentStatus.NO_ANALYSIS_VIDEO:
            case PublicContentStatus.SKIPPED_PATH:
                {
                    break;
                }
            default: throw new ArgumentException($"{nameof(VisitResponse)} is invalid", nameof(visitResponse));
        }

        VisitResponse = StringHelper.Minimize(checkVisitResponse);
    }
}