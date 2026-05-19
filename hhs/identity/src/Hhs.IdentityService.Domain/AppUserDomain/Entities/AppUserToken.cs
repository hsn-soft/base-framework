using Microsoft.AspNetCore.Identity;

namespace Hhs.IdentityService.Domain.AppUserDomain.Entities;

public sealed class AppUserToken : IdentityUserToken<Guid>
{
}