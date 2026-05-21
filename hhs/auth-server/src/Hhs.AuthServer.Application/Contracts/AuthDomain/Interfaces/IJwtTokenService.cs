using Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;
using Hhs.IdentityService.Domain.AuthDomain.Entities;

namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(AppUser user);
}