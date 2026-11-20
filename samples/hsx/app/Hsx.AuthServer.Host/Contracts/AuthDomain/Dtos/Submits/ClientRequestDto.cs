using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Logging.Masking;

namespace Hsx.AuthServer.Contracts.AuthDomain.Dtos.Submits;

/// <summary>What every request for a token says about the application that asks: who it is, its secret when it has one, and (optionally) the services it wants the token for.</summary>
public abstract class ClientRequestDto
{
    /// <summary>The application that asks (a client of <c>Auth:Clients</c>): what it may reach, and whether it needs a secret, is its own.</summary>
    [Required]
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The secret of the client, for a client that has one (a machine; a public page or app has none).</summary>
    [StringLength(256)]
    [SensitiveData]
    public string? ClientSecret { get; set; }

    /// <summary>The services the token is for: fewer than the client may reach, never more. Not given: all of them.</summary>
    [MaxLength(20)]
    public List<string>? Audiences { get; set; }
}
