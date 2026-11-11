using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Contracts.AuthDomain.Dtos.Submits;

/// <summary>A sign-out. A client that keeps the refresh token in the body (a mobile application) says which token to end; a browser sends nothing and the cookie is the token.</summary>
public sealed class LogoutDto
{
    [StringLength(512)]
    [SensitiveData]
    public string? RefreshToken { get; set; }
}
