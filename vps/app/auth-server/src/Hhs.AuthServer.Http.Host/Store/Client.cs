
namespace Hhs.AuthServer.Store;

public class Client
{
    public bool Enabled { get; set; } = true;
    
    public string ClientId { get; set; }

    public ICollection<Secret> ClientSecrets { get; set; } = new HashSet<Secret>();
    
    public bool RequireClientSecret { get; set; } = true;
    
    public string ClientName { get; set; }

    public bool AllowOfflineAccess { get; set; } = false;
    
    public ICollection<string> AllowedScopes { get; set; } = new HashSet<string>();
    
    public int AccessTokenLifetime { get; set; } = 3600;
    
}