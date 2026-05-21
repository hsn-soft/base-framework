using System.Security.Claims;
using Hhs.AuthServer.Application.Contracts.AuthDomain.Dtos;
using Hhs.AuthServer.Application.Contracts.AuthDomain.Interfaces;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AuthServer.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var userId = await _authService.RegisterAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(new { userId });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var test = _currentUser.Id;

        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            TenantId = User.FindFirstValue("tenant_id"),
            IsSystemTenant = User.FindFirstValue("is_system_tenant"),
            UserName = User.Identity?.Name,
            Email = User.FindFirstValue(ClaimTypes.Email),
            SecurityStamp = User.FindFirstValue("security_stamp"),
            Roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value),
            Claims = User.Claims.Select(x => new { x.Type, x.Value })
        });
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _authService.GetUsersAsync();
        return Ok(result);
    }

    [HttpGet("users/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _authService.GetUserAsync(id);
        return Ok(result);
    }

    [HttpPost("users")]
    [Authorize]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var userId = await _authService.CreateUserAsync(request);
        return Ok(new { userId });
    }

    [HttpPut("users/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        await _authService.UpdateUserAsync(id, request);
        return Ok();
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _authService.DeleteUserAsync(id);
        return Ok();
    }
}