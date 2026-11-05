using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppMenuPermissionRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AppMenuPermission, Guid>(db), IAppMenuPermissionRepository;
