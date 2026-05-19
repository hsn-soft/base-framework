using HsnSoft.Base.Serilog.Mask;
using JetBrains.Annotations;

namespace Hhs.AuthServer.Models;

public sealed class JwtTokenResponseDto
{
    [SensitiveData]
    public string AccessToken { get; set; }

    public int ExpiresIn { get; set; }

    public string TokenType { get; } = Constants.TokenResponse.BearerTokenType;

    [CanBeNull]
    public string Scope { get; set; }
}