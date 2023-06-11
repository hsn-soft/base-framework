using System;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy;

public class TenantConfigurationProvider : ITenantConfigurationProvider, ITransientDependency
{
    public TenantConfigurationProvider(
        ITenantResolver tenantResolver,
        ITenantStore tenantStore,
        ITenantResolveResultAccessor tenantResolveResultAccessor)
    {
        TenantResolver = tenantResolver;
        TenantStore = tenantStore;
        TenantResolveResultAccessor = tenantResolveResultAccessor;
    }

    protected virtual ITenantResolver TenantResolver { get; }
    protected virtual ITenantStore TenantStore { get; }
    protected virtual ITenantResolveResultAccessor TenantResolveResultAccessor { get; }

    public virtual async Task<TenantConfiguration> GetAsync(bool saveResolveResult = false)
    {
        var resolveResult = await TenantResolver.ResolveTenantIdOrNameAsync();

        if (saveResolveResult)
        {
            TenantResolveResultAccessor.Result = resolveResult;
        }

        TenantConfiguration tenant = null;
        if (resolveResult.TenantIdOrName != null)
        {
            tenant = await FindTenantAsync(resolveResult.TenantIdOrName);

            if (tenant == null)
            {
                throw new BusinessException(
                    errorCode: "HsnSoft.BaseIo.MultiTenancy:010001",
                    errorMessage: "Tenant not found!"
                ).WithData("reference", resolveResult.TenantIdOrName);
            }

            if (!tenant.IsActive)
            {
                throw new BusinessException(
                    errorCode: "HsnSoft.BaseIo.MultiTenancy:010002",
                    errorMessage: "Tenant not active!"
                ).WithData("reference", resolveResult.TenantIdOrName);
            }
        }

        return tenant;
    }

    protected virtual async Task<TenantConfiguration> FindTenantAsync(string tenantIdOrName)
    {
        if (Guid.TryParse(tenantIdOrName, out var parsedTenantId))
        {
            return await TenantStore.FindAsync(parsedTenantId);
        }

        return await TenantStore.FindAsync(tenantIdOrName);
    }
}