using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos;

public sealed class TokenDto : IBaseDto
{
    [SensitiveData]
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    /// <summary>How many seconds the access token lives.</summary>
    public int ExpiresIn { get; init; }

    /// <summary>The user signed in with a temporary password: it must set its own before it goes on (identity ADR-0018 and ADR-0020).</summary>
    public bool PasswordChangeRequired { get; init; }
}
