using System.Net;
using System.Security.Claims;
using Hhs.AuthServer.Application.Exceptions;
using Hhs.AuthServer.Controllers.Base;
using Hhs.AuthServer.Models;
using Hhs.AuthServer.Store;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AuthServer.Controllers;

// [SecurityHeaders]
[ControllerName("Auth")]
[Route("api/auth-server/v1/auth")]
public sealed class AuthController : BaseServiceController //, IAuthAppService
{
    // private readonly UserManager<AppUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly RedisService _redis;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAppRoleRepository _appRoleRepository;
    private readonly IAppUserRoleRepository _appUserRoleRepository;
    private readonly IDataFilter _dataFilter;

    private const int ExpirationSeconds = 30 * 60;

    public AuthController(TokenService tokenService, IServiceProvider provider, RedisService redis,
        IAppUserRepository appUserRepository, IPasswordHasher passwordHasher,
        ITenantRepository tenantRepository, IAppRoleRepository appRoleRepository, IAppUserRoleRepository appUserRoleRepository,
        IDataFilter dataFilter) : base(provider)
    {
        _tokenService = tokenService;
        _redis = redis;
        _appUserRepository = appUserRepository;
        _passwordHasher = passwordHasher;
        _tenantRepository = tenantRepository;
        _appRoleRepository = appRoleRepository;
        _appUserRoleRepository = appUserRoleRepository;
        _dataFilter = dataFilter;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<JwtTokenResponseDto> TokenAsync([FromBody] JwtPasswordTokenRequestDto input)
    {
        if (input == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        if (!input.GrantType.Equals(Constants.GrantTypes.Password) && !input.GrantType.Equals(Constants.GrantTypes.ClientCredentials))
        {
            throw new InvalidGrantTypeException(L, input.GrantType);
        }

        var client = JwtClients.Clients.FirstOrDefault(x => x.ClientId.Equals(input.ClientId));
        if (client is { Enabled: true })
        {
            // Check secret
            if (client.RequireClientSecret)
            {
                var secrets = client.ClientSecrets
                    .Where(x => (x.Expiration == null) || (x.Expiration != null && x.Expiration.Value < DateTime.Now))
                    .Select(s => s.Value)
                    .ToList();
                if (!(secrets is { Count: > 0 } && secrets.Any(x => x.Equals(input.ClientSecret.Sha256()))))
                {
                    throw new InvalidClientCredentialsException(L, input.ClientId);
                }
            }
        }
        else throw new InvalidClientCredentialsException(L, input.ClientId);

        int tokenExpireSeconds = client.AccessTokenLifetime > 0 ? client.AccessTokenLifetime : ExpirationSeconds;

        bool hasOfflineAccess = false;
        List<string> clientReturnScopes = null;
        if (string.IsNullOrWhiteSpace(input.Scope))
        {
            // Get all scopes for client
            if (client.AllowedScopes is { Count: > 0 })
            {
                clientReturnScopes = client.AllowedScopes.ToList();
                hasOfflineAccess = client.AllowedScopes.Contains(Constants.StandardScopes.OfflineAccess) || client.AllowOfflineAccess;
            }
        }
        else
        {
            // Check selected scopes
            bool hasError = input.Scope.Split(" ").Any(scope => !client.AllowedScopes.Contains(scope));
            if (hasError) throw new InvalidScopeRequestException(L, input.Scope);
            clientReturnScopes = input.Scope.Split(" ").ToList();

            // Check selected scope contains OfflineAccess
            hasOfflineAccess = input.Scope.Split(" ").Any(x => x.Equals(Constants.StandardScopes.OfflineAccess));
        }

        string accessToken;
        Guid userId;
        if (input.GrantType.Equals(Constants.GrantTypes.Password))
        {
            input.Email = StringOperations.Normalize(input.Email);

            List<AppUser> mailList = null;
            AppUser managedUser = null;
            Tenant checkedTenant = null;
            using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
            {
                mailList = await _appUserRepository.GetListAsync(new ListQueryOptions<AppUser>() { Filter = x => x.NormalizedEmail == input.Email });
            }

            if (mailList is { Count: > 0 })
            {
                if (mailList.Count == 1)
                {
                    managedUser = mailList[0];
                }
                else
                {
                    // check tenant
                    input.TenantName = StringOperations.Normalize(input.TenantName);

                    checkedTenant = await _tenantRepository.GetSingleOrDefaultAsync(x => x.NormalizedName == input.TenantName);
                    if (checkedTenant == null)
                    {
                        throw new InvalidUserCredentialsException(L, input.TenantName);
                    }

                    managedUser = mailList.FirstOrDefault(x => x.TenantId == checkedTenant.Id);
                    if (managedUser == null)
                    {
                        throw new InvalidUserCredentialsException(L, input.TenantName);
                    }
                }
            }
            else
            {
                throw new InvalidUserCredentialsException(L, input.Email);
            }

            if (managedUser.LockoutEndAt.HasValue && managedUser.LockoutEndAt > DateTime.UtcNow)
            {
                // await AddLoginAuditAsync(managedUser.TenantId, managedUser.Id, input.Email, false, "USER_LOCKED",  HttpContext.Connection.RemoteIpAddress?.ToString(),  Request.Headers.UserAgent.ToString());
                // throw new UnauthorizedAccessException("Kullanıcı kilitli.");
                throw new InvalidUserCredentialsException(L, input.Email);
            }

            // TODO : DEVAM

            bool isPasswordValid = _passwordHasher.Verify(input.Password, managedUser.PasswordHash);
            ;
            if (!isPasswordValid)
            {
                // await IncreaseFailedLoginAsync(user);
                // await AddLoginAuditAsync(user.TenantId, user.Id, request.UserEmail, false, "INVALID_PASSWORD", ipAddress, userAgent);
                throw new InvalidUserCredentialsException(L, input.Email);
            }

            List<string> roleNames = [];
            var userRoleIds = await _appUserRoleRepository.GetListAsync(new ListQueryOptions<AppUserRole>() { Filter = x => x.UserId == managedUser.Id }, s => s.RoleId);
            if (userRoleIds is { Count: > 0 })
            {
                using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
                {
                    roleNames = await _appRoleRepository.GetListAsync(new ListQueryOptions<AppRole>() { Filter = x => userRoleIds.Contains(x.Id) }, s => s.NormalizedName);
                }
            }

            accessToken = _tokenService.CreateUserToken(managedUser, roleNames, client, clientReturnScopes, tokenExpireSeconds, input.ClientSecret);

            if (hasOfflineAccess)
            {
                string refreshToken = _tokenService.CreateRefreshToken();
                await _redis.AddRefreshTokenAsync(managedUser.Id, refreshToken, TimeSpan.FromDays(7));

                // set refresh token response cookie
                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // HTTPS required
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }
        }
        else
        {
            accessToken = _tokenService.CreateM2MToken(client, clientReturnScopes, tokenExpireSeconds, input.ClientSecret);
        }

        return new JwtTokenResponseDto { AccessToken = accessToken, ExpiresIn = tokenExpireSeconds, Scope = clientReturnScopes is { Count: > 0 } ? clientReturnScopes.JoinAsString(" ") : null };
    }

    [AllowAnonymous]
    [HttpPost("token-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<JwtTokenResponseDto> TokenRefreshAsync([FromBody] JwtRefreshTokenRequestDto input)
    {
        // Cookie'den refresh token al
        if (!Request.Cookies.TryGetValue("refreshToken", out string refreshToken) || string.IsNullOrEmpty(refreshToken))
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "TOKEN_NOT_FOUND");

        var userId = await _redis.GetUserIdByRefreshTokenAsync(refreshToken);
        if (userId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "USER_NOT_FOUND");
        }

        // Kullanıcıyı bul
        var managedUser = await _appUserRepository.GetByIdOrDefaultAsync(userId.Value);
        if (managedUser == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "USER_NOT_FOUND");
        }

        // Redis'te refresh token geçerli mi?
        bool isValid = await _redis.IsRefreshTokenValidAsync(managedUser.Id, refreshToken);
        if (!isValid)
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "INVALID_REFRESH_TOKEN");

        var client = JwtClients.Clients.FirstOrDefault(x => x.ClientId == input.ClientId);
        if (client == null)
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "CLIENT_NOT_FOUND");


        List<string> roleNames = [];
        var userRoleIds = await _appUserRoleRepository.GetListAsync(new ListQueryOptions<AppUserRole>() { Filter = x => x.UserId == managedUser.Id }, s => s.RoleId);
        if (userRoleIds is { Count: > 0 })
        {
            using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
            {
                roleNames = await _appRoleRepository.GetListAsync(new ListQueryOptions<AppRole>() { Filter = x => userRoleIds.Contains(x.Id) }, s => s.NormalizedName);
            }
        }

        int tokenExpireSeconds = client.AccessTokenLifetime > 0 ? client.AccessTokenLifetime : ExpirationSeconds;
        var scopes = client.AllowedScopes.ToList();

        // Yeni Access Token oluştur
        string newAccessToken = _tokenService.CreateUserToken(
            managedUser,
            roleNames,
            client,
            scopes,
            tokenExpireSeconds,
            client.ClientSecrets.FirstOrDefault()?.Value
        );

        // Güvenlik için yeni refresh token üretip eskiyi kaldır
        string newRefreshToken = _tokenService.CreateRefreshToken();
        await _redis.RemoveRefreshTokenAsync(managedUser.Id, refreshToken);
        await _redis.AddRefreshTokenAsync(managedUser.Id, newRefreshToken, TimeSpan.FromDays(7));

        // Yeni cookie ayarla
        Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(7) });

        return new JwtTokenResponseDto { AccessToken = newAccessToken, ExpiresIn = tokenExpireSeconds, Scope = scopes.JoinAsString(" ") };
    }

    [Authorize]
    [HttpPost("token-logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task TokenLogoutAsync()
    {
        // Cookie'den refresh token al
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            // JWT içinden userId al
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Redis'ten bu token'ı kaldır
                await _redis.RemoveRefreshTokenAsync(Guid.Parse(userId), refreshToken);
            }

            // Cookie'yi temizle
            Response.Cookies.Append("refreshToken", "", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddDays(-1) });
        }
    }
}