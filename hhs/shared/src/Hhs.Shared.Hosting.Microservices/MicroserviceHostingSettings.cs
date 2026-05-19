using HsnSoft.Base.AspNetCore.Settings;

namespace Hhs.Shared.Hosting.Microservices;

public sealed class MicroserviceHostingSettings : HostingSettings
{
    public int CachePermissionsUpdateSeconds { get; set; } = 300;
}