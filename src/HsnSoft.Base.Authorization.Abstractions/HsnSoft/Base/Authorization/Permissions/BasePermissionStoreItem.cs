using System;

namespace HsnSoft.Base.Authorization.Permissions;

public class BasePermissionStoreItem
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; }

    public string ProviderName { get; set; }

    public string ProviderKey { get; set; }
}