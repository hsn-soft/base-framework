namespace Hhs.AuthServer.Store;

public class Secret
{
    public string Value { get; set; }

    public DateTime? Expiration { get; set; }

    public Secret()
    {
    }

    public Secret(string value, DateTime? expiration = null)
        : this()
    {
        Value = value;
        Expiration = expiration;
    }
}