using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;

/// <summary>A user signs in through a client (the application it uses).</summary>
public sealed class LoginDto : ClientRequestDto
{
    /// <summary>The tenant the user belongs to (its name): an email or a user name is unique only inside a tenant.</summary>
    [Required]
    [StringLength(100)]
    public string Tenant { get; set; } = string.Empty;

    /// <summary>The email or the user name of the user.</summary>
    [Required]
    [StringLength(256)]
    public string Account { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [SensitiveData]
    public string Password { get; set; } = string.Empty;

    /// <summary>Asks for a refresh token too (kept in an HttpOnly cookie), to get a new access token without the password. The client has to allow it.</summary>
    public bool OfflineAccess { get; set; }
}
