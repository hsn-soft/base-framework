namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class SwaggerAggregationOptions
{
    public List<SwaggerServiceDefinition> Services { get; set; } = [];

    /// <summary>
    /// Rewritten swagger json cache süresi.
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Downstream swagger çağrısının timeout süresi.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// İlk deneme hariç kaç ek retry yapılacak.
    /// Örn: 2 => toplam 3 deneme
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// Retry bekleme süresi.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 300;
}