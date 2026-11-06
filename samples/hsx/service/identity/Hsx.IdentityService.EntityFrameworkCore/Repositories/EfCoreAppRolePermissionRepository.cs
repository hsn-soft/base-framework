using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppRolePermissionRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AppRolePermission, Guid>(db), IAppRolePermissionRepository;
