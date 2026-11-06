using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreCompanyRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, Company, Guid>(db), ICompanyRepository;
