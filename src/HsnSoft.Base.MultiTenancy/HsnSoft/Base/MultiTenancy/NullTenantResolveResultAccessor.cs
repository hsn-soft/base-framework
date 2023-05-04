using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public class NullTenantResolveResultAccessor : ITenantResolveResultAccessor, ISingletonDependency
{
    public TenantResolveResult Result
    {
        get => null;
        set { }
    }
}