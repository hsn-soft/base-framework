using Abc.IdentityService.Application.Contracts.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;

public sealed class GetUsersPaged : PagedDataRequestDto
{
    /// <summary>Only the active (<see langword="true"/>) or only the inactive (<see langword="false"/>) users; all when it is not given.</summary>
    public bool? IsActive { get; set; }
}
