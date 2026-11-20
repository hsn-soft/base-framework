using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Contracts.AuthDomain.Dtos;

public sealed class TokenDto : IBaseDto
{
    [SensitiveData]
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    /// <summary>How many seconds the access token lives.</summary>
    public int ExpiresIn { get; init; }

    /// <summary>The refresh token, for a client that keeps it in the body (a mobile application: store it in the secure storage of the device and send it back to <c>token-refresh</c>; every refresh gives a new one and ends this one). Nothing for a client that has it as a cookie, and for a machine.</summary>
    [SensitiveData]
    public string? RefreshToken { get; init; }

    /// <summary>The user signed in with a temporary password: it must set its own before it goes on (identity ADR-0018 and ADR-0020).</summary>
    public bool PasswordChangeRequired { get; init; }
}
