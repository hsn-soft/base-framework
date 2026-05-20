using Hhs.IdentityService.Application.Contracts.AuthDomain.Dtos;
using Hhs.IdentityService.Domain.AuthDomain.Entities;

namespace Hhs.IdentityService.Application.Contracts.AuthDomain.Interfaces;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(AppUser user);
}