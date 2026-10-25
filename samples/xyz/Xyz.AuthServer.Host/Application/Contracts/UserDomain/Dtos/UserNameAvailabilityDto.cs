namespace Xyz.AuthServer.Application.Contracts.UserDomain.Dtos;

public sealed class UserNameAvailabilityDto
{
    public string UserName { get; init; } = string.Empty;

    public bool IsAvailable { get; init; }
}
