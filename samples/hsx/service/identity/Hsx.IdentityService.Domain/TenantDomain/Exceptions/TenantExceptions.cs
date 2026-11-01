using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.TenantDomain.Exceptions;

/// <summary>There is no such tenant (or the caller may not see it): answered as 404.</summary>
public static class TenantNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Tenant {id} was not found.").WithKey("tenant_not_found", id);
}

/// <summary>There is a tenant with this name already.</summary>
public sealed class TenantNameDuplicateException : BusinessException
{
    public const string Code = "tenant_name_duplicate";

    public TenantNameDuplicateException(string name)
        : base(Code, $"A tenant named '{name}' exists.") => WithKey(Code, name);
}

/// <summary>A tenant cannot be put under itself or under one of the tenants under it (the tree would have a loop).</summary>
public sealed class TenantLoopException : BusinessException
{
    public const string Code = "tenant_loop";

    public TenantLoopException()
        : base(Code, "A tenant cannot be put under itself or under one of its own tenants.") => WithKey(Code);
}

/// <summary>A tenant that has tenants or users under it is not deleted.</summary>
public sealed class TenantNotEmptyException : BusinessException
{
    public const string Code = "tenant_not_empty";

    public TenantNotEmptyException(Guid id)
        : base(Code, $"Tenant {id} has tenants or users and cannot be deleted.") => WithKey(Code, id);
}
