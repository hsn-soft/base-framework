using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCorePermissionRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, Permission, Guid>(db), IPermissionRepository;
