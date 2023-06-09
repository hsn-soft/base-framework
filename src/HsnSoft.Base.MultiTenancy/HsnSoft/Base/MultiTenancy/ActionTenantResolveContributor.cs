using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class ActionTenantResolveContributor : TenantResolveContributorBase
{
    public const string ContributorName = "Action";

    private readonly Action<ITenantResolveContext> _resolveAction;

    public ActionTenantResolveContributor([NotNull] Action<ITenantResolveContext> resolveAction)
    {
        Check.NotNull(resolveAction, nameof(resolveAction));

        _resolveAction = resolveAction;
    }

    public override string Name => ContributorName;

    public override Task ResolveAsync(ITenantResolveContext context)
    {
        _resolveAction(context);
        return Task.CompletedTask;
    }
}