using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleSearchDto : EntityDto<Guid>
{
    [NotNull] public string TenantName { get; set; }= string.Empty;
    [NotNull] public string Name { get; set; } = string.Empty;
}