using HsnSoft.Base.Serilog.Mask;
using JetBrains.Annotations;

namespace Hhs.AuthServer.Models;

public abstract class BaseTokenRequestDto
{
    [CanBeNull]
    public string ClientId { get; set; }

    [SensitiveData]
    [CanBeNull]
    public string ClientSecret { get; set; }

    [CanBeNull]
    public string GrantType { get; set; } = Constants.GrantTypes.Password;

    [CanBeNull]
    public string Scope { get; set; }
}