using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;

namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Repositories;

/// <summary>The repository of the sign-in audits: they are written for every attempt and never changed, so the generic repository is what there is to use.</summary>
public interface IAuthLoginAuditRepository : IRepository<AuthLoginAudit, Guid>;
