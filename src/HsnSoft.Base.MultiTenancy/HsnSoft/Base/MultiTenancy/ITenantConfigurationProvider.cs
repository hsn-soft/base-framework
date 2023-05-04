using System.Threading.Tasks;

namespace HsnSoft.Base.MultiTenancy;

public interface ITenantConfigurationProvider
{
    Task<TenantConfiguration> GetAsync(bool saveResolveResult = false);
}
