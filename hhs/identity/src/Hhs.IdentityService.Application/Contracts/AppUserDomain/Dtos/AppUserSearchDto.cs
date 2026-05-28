using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;

public sealed class AppUserSearchDto : EntityDto<Guid>
{
    [NotNull] public string TenantName { get; set; }= string.Empty;
    [NotNull] public string UserName { get; set; } = string.Empty;
    [NotNull] public string Email { get; set; } = string.Empty;
}