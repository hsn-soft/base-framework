using HsnSoft.Base.Application.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos;

/// <summary>What a search shows of a user: enough to recognize and pick one.</summary>
public sealed class UserSearchDto : EntityDto<Guid>
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
