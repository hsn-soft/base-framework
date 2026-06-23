using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;

public sealed class GetAppUsersFilter : SortedAndLimitedDataRequestDto
{
    public Guid? TenantId { get; set; } = null;

    [CanBeNull]
    public string UserName { get; set; } = null;

    [CanBeNull]
    public string Email { get; set; } = null;

    public bool? EmailConfirmed { get; set; } = null;

    [CanBeNull]
    public string PhoneNumber { get; set; } = null;

    public bool? PhoneNumberConfirmed { get; set; } = null;
}