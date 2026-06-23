using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.AdministrationService.Domain.MenuDomain.Repositories;

public interface IAppMenuPermissionRepository : IGenericRepository<AppMenuPermission, Guid>
{
}