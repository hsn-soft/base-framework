namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos;

/// <summary>What a search shows of a user: enough to recognize and pick one.</summary>
public sealed class UserSearchDto
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
