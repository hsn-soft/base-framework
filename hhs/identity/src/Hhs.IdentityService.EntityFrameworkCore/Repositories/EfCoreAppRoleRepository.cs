using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppRoleRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, IdentityServiceDbContext dbContext)
    : EfCoreGenericRepository<AppRole, Guid>(provider, dbContext), IAppRoleRepository
{
}