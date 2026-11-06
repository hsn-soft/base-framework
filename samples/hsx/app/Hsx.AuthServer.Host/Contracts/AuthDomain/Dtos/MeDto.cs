using HsnSoft.Base.Application.Dtos;

namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos;

public sealed class MeDto : IBaseDto
{
    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
