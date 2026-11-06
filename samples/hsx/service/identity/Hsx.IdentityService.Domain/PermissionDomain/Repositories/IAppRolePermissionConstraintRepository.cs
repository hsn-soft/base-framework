using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Repositories;

/// <summary>The repository of the constraints the roles were granted, with their values. The standard reads and writes are in the generic repository; a method the standard ones do not have is added here.</summary>
public interface IAppRolePermissionConstraintRepository : IRepository<AppRolePermissionConstraint, Guid>;
