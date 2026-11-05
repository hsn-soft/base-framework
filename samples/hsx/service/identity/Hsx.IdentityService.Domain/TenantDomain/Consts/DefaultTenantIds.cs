using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Domain.TenantDomain.Consts;

/// <summary>The ids of the tenants the sample starts with (the same everywhere, so the data that names them moves between environments): the system and the resellers (<see cref="HsxTenants"/>).</summary>
public static class DefaultTenantIds
{
    public static readonly Guid System = HsxTenants.System;

    public static readonly Guid TechSummus = HsxTenants.TechSummus;

    public static readonly Guid AudioTime = HsxTenants.AudioTime;

    public static readonly Guid EPartner = HsxTenants.EPartner;
}
