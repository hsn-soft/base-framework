using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public interface ITenantResolveResultAccessor
{
    [CanBeNull]
    TenantResolveResult Result { get; set; }
}
