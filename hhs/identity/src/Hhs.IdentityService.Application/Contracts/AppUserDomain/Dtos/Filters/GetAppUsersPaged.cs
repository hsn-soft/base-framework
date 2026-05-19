using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;

public sealed class GetAppUsersPaged : PagedDataRequestDto
{
    public Guid? TenantId { get; set; } = null;

    // Default identity model
    [CanBeNull]
    public string UserName { get; set; } = null;

    [CanBeNull]
    public string Email { get; set; } = null;

    public bool? EmailConfirmed { get; set; } = null;

    [CanBeNull]
    public string PhoneNumber { get; set; } = null;

    public bool? PhoneNumberConfirmed { get; set; } = null;

    // Custom identity model
    [CanBeNull]
    public string Name { get; set; } = null;

    [CanBeNull]
    public string Surname { get; set; } = null;

    public List<RoleSearchResult> Roles { get; set; } = null;
}

public class RoleSearchResult
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; }
}