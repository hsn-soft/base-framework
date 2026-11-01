using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCorePasswordPolicyProvider(IdentityDbContext db) : IPasswordPolicyProvider
{
    public async Task<PasswordRules> GetRulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        AuthPasswordPolicy? policy = await db.AuthPasswordPolicies.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        return policy?.ToRules() ?? PasswordRules.Default;
    }
}
