using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;

namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;

/// <summary>The rules of a tenant: its own policy, else <see cref="PasswordRules.Default"/>. The tenant is named, not the current one: the sign-in does not know a tenant until it has found the user.</summary>
public interface IAuthPasswordPolicyRepository : IRepository<AuthPasswordPolicy, Guid>
{
    Task<PasswordRules> GetRulesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
