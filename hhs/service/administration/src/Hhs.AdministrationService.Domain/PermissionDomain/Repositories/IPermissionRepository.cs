using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Repositories;

public interface IPermissionRepository : IGenericRepository<Permission, Guid>
{
}