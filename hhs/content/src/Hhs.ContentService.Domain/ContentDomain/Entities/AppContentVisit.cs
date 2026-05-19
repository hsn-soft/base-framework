using System.Globalization;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AppContentVisit : Entity<Guid>
{
    public Guid ClientId { get; private set; }

    public Guid AppContentId { get; private set; }

    public uint VisitTimeLine { get; private set; }

    [NotNull]
    public string VisitResponse { get; private set; }

    private AppContentVisit()
    {
        VisitResponse = string.Empty;
        VisitTimeLine = uint.Parse(DateTime.UtcNow.ToString("yyMMddHHmm"));
    }

    internal AppContentVisit(Guid id, Guid clientId, Guid appContentId, [NotNull] string visitResponse) : this()
    {
        Id = id;
        SetClientId(clientId);
        AppContentId = appContentId;
        SetVisitResponse(visitResponse);
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    internal void SetVisitResponse(string visitResponse)
    {
        string result = Check.NotNull(visitResponse, nameof(visitResponse), AppContentVisitConsts.VisitResponseMaxLength);

        switch (result)
        {
            case AppContentPublicStatus.READY: { break; }
            case AppContentPublicStatus.CREATED: { break; }
            case AppContentPublicStatus.NO_ANALYSIS_VIDEO: { break; }
            case AppContentPublicStatus.SKIPPED_PATH: { break; }
            default: throw new ArgumentException($"{nameof(VisitResponse)} is invalid", nameof(visitResponse));
        }

        VisitResponse = result.ToLower(new CultureInfo("en-US"));
    }
}