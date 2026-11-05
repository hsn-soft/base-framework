using System.Reflection;

namespace Hsx.IdentityService.EntityFrameworkCore;

/// <summary>Names this assembly for what scans assemblies (the handlers of the service, the localization texts). The first code of the project goes beside it; nothing else here yet.</summary>
public static class AssemblyMarker
{
    public static Assembly Assembly => typeof(AssemblyMarker).Assembly;
}
