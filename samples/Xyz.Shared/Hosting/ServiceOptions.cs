namespace Xyz.Shared.Hosting;

public sealed class ServiceOptions
{
    /// <summary>The logical name of the running service, e.g. "order-service". It prefixes the broker queues.</summary>
    public string Name { get; set; } = "service";
}
