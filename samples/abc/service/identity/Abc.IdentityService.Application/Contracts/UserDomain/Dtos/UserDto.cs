using HsnSoft.Base.Application.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos;

public sealed class UserDto : AuditedEntityDto<Guid>
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Language { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsActive { get; init; }

    public Guid TenantId { get; init; }

}
