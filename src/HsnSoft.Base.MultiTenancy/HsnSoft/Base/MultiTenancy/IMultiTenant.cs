using System;

namespace HsnSoft.Base.MultiTenancy;

public interface IMultiTenant
{
    /// <summary>
    /// Id of the related tenant.
    /// </summary>
    Guid? TenantId { get; }
}
