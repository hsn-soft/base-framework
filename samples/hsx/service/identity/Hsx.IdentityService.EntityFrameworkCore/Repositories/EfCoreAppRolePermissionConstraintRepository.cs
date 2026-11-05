using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppRolePermissionConstraintRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AppRolePermissionConstraint, Guid>(db), IAppRolePermissionConstraintRepository;
