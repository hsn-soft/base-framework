using Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;

namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;

public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent);
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string refreshToken);
    Task<List<object>> GetUsersAsync();
    Task<object> GetUserAsync(Guid id);
    Task<Guid> CreateUserAsync(CreateUserRequest request);
    Task UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task DeleteUserAsync(Guid id);
}