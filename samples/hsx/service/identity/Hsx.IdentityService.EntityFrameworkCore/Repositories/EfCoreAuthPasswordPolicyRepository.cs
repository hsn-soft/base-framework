using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAuthPasswordPolicyRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, AuthPasswordPolicy, Guid>(db), IAuthPasswordPolicyRepository
{
    /// <summary>The tenant is named, not the current one: the sign-in does not know a tenant until it has found the user, so the filter of the tenants is off.</summary>
    public async Task<PasswordRules> GetRulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            AuthPasswordPolicy? policy = await FindAsync(candidate => candidate.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
            return policy?.ToRules() ?? PasswordRules.Default;
        }
    }
}
