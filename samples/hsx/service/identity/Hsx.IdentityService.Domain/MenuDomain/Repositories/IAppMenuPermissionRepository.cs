using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.MenuDomain.Entities;

namespace Hsx.IdentityService.Domain.MenuDomain.Repositories;

/// <summary>The repository of the permissions of the menu items. The standard reads and writes are in the generic repository; a method the standard ones do not have is added here.</summary>
public interface IAppMenuPermissionRepository : IRepository<AppMenuPermission, Guid>;
