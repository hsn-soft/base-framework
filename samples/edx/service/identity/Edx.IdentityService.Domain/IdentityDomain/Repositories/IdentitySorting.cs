using Edx.IdentityService.Domain.IdentityDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Edx.IdentityService.Domain.IdentityDomain.Repositories;

/// <summary>The fields the lists may be sorted by (the names a client sends) and the sorting when it sends none.</summary>
public static class IdentitySorting
{
    public const string Default = "name asc";

    public static readonly SortFields<User> Users = new SortFields<User>()
        .Add("name", user => user.NormalizedUserName)
        .Add("email", user => user.Email)
        .Add("isActive", user => user.IsActive)
        .Add("createdAt", user => user.CreatedAt);

    public static readonly SortFields<Role> Roles = new SortFields<Role>()
        .Add("name", role => role.NormalizedName);

    public static readonly SortFields<Tenant> Tenants = new SortFields<Tenant>()
        .Add("name", tenant => tenant.Name)
        .Add("code", tenant => tenant.Code);
}
