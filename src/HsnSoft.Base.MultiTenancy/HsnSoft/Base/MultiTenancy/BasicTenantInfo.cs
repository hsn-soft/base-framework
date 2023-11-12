using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public class BasicTenantInfo
{
    /// <summary>
    /// Null indicates the host.
    /// Not null value for a tenant.
    /// </summary>
    [CanBeNull]
    public Guid? TenantId { get; }

    /// <summary>
    /// Name of the tenant if <see cref="TenantId"/> is not null.
    /// </summary>
    [CanBeNull]
    public string Name { get; }

    [CanBeNull]
    public string Domain { get; }

    public BasicTenantInfo(Guid? tenantId, string name = null,string domain = null)
    {
        TenantId = tenantId;
        Name = name;
        Domain = domain;
    }
}
