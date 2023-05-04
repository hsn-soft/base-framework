using System.Threading.Tasks;

namespace HsnSoft.Base.MultiTenancy;

public interface ITenantResolveContributor
{
    string Name { get; }

    Task ResolveAsync(ITenantResolveContext context);
}
