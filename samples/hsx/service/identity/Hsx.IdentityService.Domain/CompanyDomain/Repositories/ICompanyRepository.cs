using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;

namespace Hsx.IdentityService.Domain.CompanyDomain.Repositories;

/// <summary>The repository of the companies. The standard reads and writes are in the generic repository; a method the standard ones do not have is added here.</summary>
public interface ICompanyRepository : IRepository<Company, Guid>;
