using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppUserRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, IdentityServiceDbContext dbContext)
    : EfCoreGenericRepository<AppUser, Guid>(provider, dbContext), IAppUserRepository
{
}