namespace Hhs.FeedRService.Domain.ConfigurationDomain.Entities;

/// <summary>
/// Value object representing a single client (AdUnit) within a <see cref="TopLevelGroupConfig"/>.
/// </summary>
public sealed class ClientConfig
{
    public string AdUnitCode { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;

    public ClientConfig() { }

    public ClientConfig(string adUnitCode, Guid clientId, string clientName)
    {
        AdUnitCode = adUnitCode ?? string.Empty;
        ClientId = clientId;
        ClientName = clientName ?? string.Empty;
    }
}
