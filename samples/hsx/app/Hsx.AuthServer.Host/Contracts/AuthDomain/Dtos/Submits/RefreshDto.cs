using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Contracts.AuthDomain.Dtos.Submits;

/// <summary>A new access token for a refresh token, through a client (the one the user signed in through or another that allows it): the token is in the cookie, or in <see cref="RefreshToken"/> for a client that keeps it in the body.</summary>
public sealed class RefreshDto : ClientRequestDto
{
    /// <summary>The refresh token, for a client that keeps it in the body (a mobile application). A client that has the cookie sends none.</summary>
    [StringLength(512)]
    [SensitiveData]
    public string? RefreshToken { get; set; }
}
