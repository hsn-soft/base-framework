namespace Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;

public sealed class RegisterRequest
{
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public sealed class LoginRequest
{
    public string UserEmail { get; set; } = null!;
    public string UserPassword { get; set; } = null!;
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}

public sealed class CreateUserRequest
{
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class UpdateUserRequest
{
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
}