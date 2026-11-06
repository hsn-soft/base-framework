using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAuthLoginAuditRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AuthLoginAudit, Guid>(db), IAuthLoginAuditRepository;
