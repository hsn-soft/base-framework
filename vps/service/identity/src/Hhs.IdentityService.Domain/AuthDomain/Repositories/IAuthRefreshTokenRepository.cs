using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAuthRefreshTokenRepository : IGenericRepository<AuthRefreshToken, Guid>
{
    Task RevokeRefreshTokenAsync(string refreshTokenHash,string replacedByTokenHash);
}