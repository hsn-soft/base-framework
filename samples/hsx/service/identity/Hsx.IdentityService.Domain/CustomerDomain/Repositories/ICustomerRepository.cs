using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;

namespace Hsx.IdentityService.Domain.CustomerDomain.Repositories;

/// <summary>The repository of the customers. The standard reads and writes are in the generic repository; a method the standard ones do not have is added here.</summary>
public interface ICustomerRepository : IRepository<Customer, Guid>;
