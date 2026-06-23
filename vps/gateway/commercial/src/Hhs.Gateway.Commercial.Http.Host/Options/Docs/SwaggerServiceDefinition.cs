namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class SwaggerServiceDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Swagger path re-write for SwaggerUI
    public string DownstreamPathPrefix { get; set; } = string.Empty;
    public string GatewayPathPrefix { get; set; } = string.Empty;

    // Swagger Doc details
    public string? ServiceHealthPath { get; set; }
    public string? GatewayHealthPath { get; set; }
}