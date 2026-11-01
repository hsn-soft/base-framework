using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Consts;

namespace Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;

/// <summary>
/// A visit to a content, and how the site answered it. It is seen by its scope key.
/// </summary>
public sealed class CustomerContentVisit : Entity<Guid>, IScopeSubscription
{
    // for EF Core
    private CustomerContentVisit()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public Guid CustomerContentId { get; private set; }

    public DateTimeOffset VisitTime { get; private set; }

    public string VisitResponse { get; private set; } = string.Empty;

    public static CustomerContentVisit Create(string scopeKey, Guid customerContentId, string visitResponse)
    {
        CustomerContentVisit entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), CustomerContentVisitConsts.ScopeKeyMaxLength).Trim(),
            CustomerContentId = Guard.NotDefault<Guid>(customerContentId, nameof(CustomerContentId)),
            VisitResponse = Guard.NotNullOrWhiteSpace(visitResponse, nameof(VisitResponse), CustomerContentVisitConsts.VisitResponseMaxLength).Trim(),
            VisitTime = DateTimeOffset.UtcNow
        };
        return entity;
    }
}
