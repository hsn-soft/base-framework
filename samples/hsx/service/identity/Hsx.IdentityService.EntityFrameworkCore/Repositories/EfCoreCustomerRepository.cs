using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreCustomerRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, Customer, Guid>(db), ICustomerRepository;
