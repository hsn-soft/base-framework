namespace HsnSoft.Base.Caching.StackExchangeRedis;

/// <summary>Settings next to the connection string (<c>ConnectionStrings:Redis</c>); section <c>Redis</c>.</summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Prefix of every key this service writes, e.g. "myproduct:". It keeps products that share one Redis server apart.
    /// </summary>
    public string InstanceName { get; set; } = string.Empty;
}
