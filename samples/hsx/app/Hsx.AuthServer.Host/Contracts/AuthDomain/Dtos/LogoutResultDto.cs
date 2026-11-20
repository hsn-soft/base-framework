using HsnSoft.Base.Application.Dtos;

namespace Hsx.AuthServer.Contracts.AuthDomain.Dtos;

/// <summary>The answer of a sign-out: the refresh token of the cookie is ended and the cookie is removed (there is nothing to end when the request had none).</summary>
public sealed class LogoutResultDto : IBaseDto
{
    /// <summary>A refresh token was ended.</summary>
    public bool RefreshTokenRevoked { get; init; }
}
