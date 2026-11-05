using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Repositories;

/// <summary>The repository of the permissions. The standard reads and writes are in the generic repository; a method the standard ones do not have is added here.</summary>
public interface IPermissionRepository : IRepository<Permission, Guid>;
