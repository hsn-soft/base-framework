using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Tracing;

public static class ClientExtensions
{
    private const string ClientRequestLat = "X-Client-Request-Lat";
    private const string ClientRequestLong = "X-Client-Request-Long";
    private const string ClientVersion = "X-Client-Version";
    private const string Channel = "X-Channel";

    [CanBeNull]
    public static string GetChannel(this HttpContext context)
        => context.Items.TryGetValue(Channel, out var channel) ? channel as string : null;

    [CanBeNull]
    public static string GetClientRequestLat(this HttpContext context)
        => context.Items.TryGetValue(ClientRequestLat, out var clientRequestLat) ? clientRequestLat as string : null;

    [CanBeNull]
    public static string GetClientRequestLong(this HttpContext context)
        => context.Items.TryGetValue(ClientRequestLong, out var clientRequestLong) ? clientRequestLong as string : null;

    [CanBeNull]
    public static string GetClientVersion(this HttpContext context)
        => context.Items.TryGetValue(ClientVersion, out var clientVersion) ? clientVersion as string : null;
}