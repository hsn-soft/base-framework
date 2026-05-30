using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AppContentVisit : Entity<Guid>
{
    public Guid ClientId { get; private set; }

    public Guid AppContentId { get; private set; }

    public uint VisitTimeLine { get; private set; }

    [NotNull] public string VisitResponse { get; private set; }


    private AppContentVisit()
    {
        // Not-Null string fields
        VisitResponse = string.Empty;
        VisitTimeLine = uint.Parse(DateTime.UtcNow.ToString("yyMMddHHmm"));
    }

    internal AppContentVisit(Guid clientId, Guid appContentId, [NotNull] string visitResponse)
        : this(Guid.CreateVersion7(), clientId, appContentId, visitResponse)
    {
    }

    internal AppContentVisit(Guid id, Guid clientId, Guid appContentId, [NotNull] string visitResponse) : this()
    {
        Id = id;
        ClientId = clientId;
        AppContentId = appContentId;
        SetVisitResponse(visitResponse);
    }


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

        VisitResponse = StringOperations.Minimize(checkVisitResponse);
    }
}