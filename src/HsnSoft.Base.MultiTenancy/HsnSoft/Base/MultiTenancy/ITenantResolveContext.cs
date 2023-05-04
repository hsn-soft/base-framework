using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public interface ITenantResolveContext : IServiceProviderAccessor
{
    [CanBeNull]
    string TenantIdOrName { get; set; }

    bool Handled { get; set; }
}