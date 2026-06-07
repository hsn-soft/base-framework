namespace Hhs.AuthServer.Store;

public sealed class ApiScope
{
    public string Name { get; set; }
    public List<string> Resources { get; set; }
}