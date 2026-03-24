using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Tracing;

public static class ClientExtensions
{
    private const string ClientRequestLat = "X-Client-Request-Lat";
    private const string ClientRequestLong = "X-Client-Request-Long";
    private const string ClientVersion = "X-Client-Version";
    private const string Channel = "X-Channel";

    extension(HttpContext context)
    {
        [CanBeNull]
        public string GetChannel()
            => context.Request.Headers.TryGetValue(Channel, out var channel) ? channel.ToString() : null;

        [CanBeNull]
        public string GetClientRequestLat()
            => context.Request.Headers.TryGetValue(ClientRequestLat, out var clientRequestLat) ? clientRequestLat.ToString() : null;

        [CanBeNull]
        public string GetClientRequestLong()
            => context.Request.Headers.TryGetValue(ClientRequestLong, out var clientRequestLong) ? clientRequestLong.ToString() : null;

        [CanBeNull]
        public string GetClientVersion()
            => context.Request.Headers.TryGetValue(ClientVersion, out var clientVersion) ? clientVersion.ToString() : null;
    }
}