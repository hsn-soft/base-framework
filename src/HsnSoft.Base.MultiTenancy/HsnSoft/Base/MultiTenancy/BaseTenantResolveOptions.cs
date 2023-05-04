using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class BaseTenantResolveOptions
{
    [NotNull]
    public List<ITenantResolveContributor> TenantResolvers { get; }

    public BaseTenantResolveOptions()
    {
        TenantResolvers = new List<ITenantResolveContributor> { new CurrentUserTenantResolveContributor() };
    }
}