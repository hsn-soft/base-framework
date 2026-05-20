using Hhs.IdentityService.Application.Contracts.AuthDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AuthDomain.Interfaces;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;

namespace Hhs.IdentityService;

using Microsoft.EntityFrameworkCore;

public sealed class AuthService : IAuthService
{
    private const string RegisteredRoleName = "registered";

    private readonly IdentityServiceDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IDataFilter _dataFilter;

    public AuthService(
        IdentityServiceDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUser currentUser,
        IDataFilter dataFilter)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _dataFilter = dataFilter;
    }

    public async Task<Guid> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId);
        if (tenant is null)
            throw new Exception("Tenant bulunamadı.");

        await ValidatePasswordAsync(request.TenantId, request.Password);

        string normalizedUserName = Normalize(request.UserName);
        string normalizedEmail = Normalize(request.Email);

        bool exists = await _db.AppUsers.AnyAsync(x =>
            x.TenantId == request.TenantId &&
            (x.NormalizedUserName == normalizedUserName || x.NormalizedEmail == normalizedEmail));

        if (exists)
            throw new Exception("UserName veya Email zaten kullanılıyor.");

        var registeredRole = await GetOrCreateRegisteredRoleAsync(request.TenantId);

        var user = new AppUser(
            tenantId: request.TenantId,
            userName: request.UserName.Trim(),
            email: request.Email.Trim(),
            passwordHash: _passwordHasher.Hash(request.Password)
        );

        _db.AppUsers.Add(user);

        _db.AppUserRoles.Add(new AppUserRole { User = user, Role = registeredRole });

        await _db.SaveChangesAsync();

        return user.Id;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        string normalized = Normalize(request.UserEmail);

        AppUser user;
        using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            user = await _db.AppUsers
                // .Include(x => x.Tenant)
                // .FirstOrDefaultAsync(x =>
                //     x.TenantId == request.TenantId &&
                //     (x.NormalizedUserName == normalized || x.NormalizedEmail == normalized));
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalized);
        }

        if (user is null)
        {
            // await AddLoginAuditAsync(request.TenantId, null, request.UserEmail, false, "USER_NOT_FOUND", ipAddress, userAgent);
            await AddLoginAuditAsync(null, null, request.UserEmail, false, "USER_NOT_FOUND", ipAddress, userAgent);
            throw new UnauthorizedAccessException("Kullanıcı adı/email veya şifre hatalı.");
        }

        if (user.IsDeleted)
        {
            await AddLoginAuditAsync(user.TenantId, user.Id, request.UserEmail, false, "USER_PASSIVE", ipAddress, userAgent);
            throw new UnauthorizedAccessException("Kullanıcı pasif.");
        }

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow)
        {
            await AddLoginAuditAsync(user.TenantId, user.Id, request.UserEmail, false, "USER_LOCKED", ipAddress, userAgent);
            throw new UnauthorizedAccessException("Kullanıcı kilitli.");
        }

        bool passwordValid = _passwordHasher.Verify(request.UserPassword, user.PasswordHash);

        if (!passwordValid)
        {
            await IncreaseFailedLoginAsync(user);
            await AddLoginAuditAsync(user.TenantId, user.Id, request.UserEmail, false, "INVALID_PASSWORD", ipAddress, userAgent);
            throw new UnauthorizedAccessException("Kullanıcı adı/email veya şifre hatalı.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = DateTime.UtcNow;

        var response = await _jwtTokenService.CreateTokenAsync(user);

        string refreshTokenHash = TokenHelper.Sha256(response.RefreshToken);

        _db.AuthRefreshTokens.Add(new AuthRefreshToken { UserId = user.Id, TokenHash = refreshTokenHash, ExpiresAt = DateTime.UtcNow.AddDays(30) });

        await AddLoginAuditAsync(user.TenantId, user.Id, request.UserEmail, true, null, ipAddress, userAgent);

        await _db.SaveChangesAsync();

        return response;
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        string refreshTokenHash = TokenHelper.Sha256(request.RefreshToken);

        AuthRefreshToken refreshToken = null;
        using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            refreshToken = await _db.AuthRefreshTokens
                .Include(x => x.User)
                .ThenInclude(x => x.Tenant)
                .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash);
        }

        if (refreshToken is null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token geçersiz.");

        var user = refreshToken.User;

        if (user.IsDeleted)
            throw new UnauthorizedAccessException("Kullanıcı pasif.");

        var response = await _jwtTokenService.CreateTokenAsync(user);

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByTokenHash = TokenHelper.Sha256(response.RefreshToken);

        _db.AuthRefreshTokens.Add(new AuthRefreshToken { UserId = user.Id, TokenHash = TokenHelper.Sha256(response.RefreshToken), ExpiresAt = DateTime.UtcNow.AddDays(30) });

        await _db.SaveChangesAsync();

        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        string refreshTokenHash = TokenHelper.Sha256(refreshToken);

        AuthRefreshToken entity = null;
        using (_dataFilter.Disable<IMultiTenant>()) // anonymous user , unknown tenant
        {
            entity = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash);
        }

        if (entity is null)
            return;

        entity.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<object>> GetUsersAsync()
    {
        var query = _db.AppUsers
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        // middleware e eklendi !!!!!!!!!!!!!!!!!!
        // if (!_currentUser.IsSystemTenant)
        // {
        //     if (_currentUser.TenantId is null)
        //         throw new UnauthorizedAccessException();
        //
        //     // query = query.Where(x => x.TenantId == _currentUser.TenantId);
        //     query = query.Where(x => _currentUser.AllowedTenantIds.Contains(x.TenantId));
        // }

        return await query
            .OrderByDescending(x => x.CreationTime)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                TenantName = x.Tenant.Name,
                x.UserName,
                x.Email,
                x.IsDeleted,
                x.EmailConfirmed,
                x.FailedLoginCount,
                x.LockoutEndAt,
                x.CreationTime,
                x.LastLoginAt,
                Roles = x.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<object> GetUserAsync(Guid id)
    {
        var query = _db.AppUsers
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsQueryable();

        if (!_currentUser.IsSystemTenant)
            query = query.Where(x => x.TenantId == _currentUser.TenantId);

        var user = await query.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            throw new Exception("Kullanıcı bulunamadı.");

        return new
        {
            user.Id,
            user.TenantId,
            TenantName = user.Tenant.Name,
            user.UserName,
            user.Email,
            user.IsDeleted,
            user.EmailConfirmed,
            user.FailedLoginCount,
            user.LockoutEndAt,
            user.CreationTime,
            user.LastLoginAt,
            Roles = user.UserRoles.Select(x => new { x.Role.Id, x.Role.Name })
        };
    }

    public async Task<Guid> CreateUserAsync(CreateUserRequest request)
    {
        if (!_currentUser.IsSystemTenant && request.TenantId != _currentUser.TenantId)
            throw new UnauthorizedAccessException();

        if (request.RoleIds.Count == 0)
            throw new Exception("Kullanıcının en az bir rolü olmalıdır.");

        await ValidatePasswordAsync(request.TenantId, request.Password);

        string normalizedUserName = Normalize(request.UserName);
        string normalizedEmail = Normalize(request.Email);

        bool exists = await _db.AppUsers.AnyAsync(x =>
            x.TenantId == request.TenantId &&
            (x.NormalizedUserName == normalizedUserName || x.NormalizedEmail == normalizedEmail));

        if (exists)
            throw new Exception("UserName veya Email zaten kullanılıyor.");

        var roles = await _db.AppRoles
            .Where(x => x.TenantId == request.TenantId && request.RoleIds.Contains(x.Id))
            .ToListAsync();

        if (roles.Count != request.RoleIds.Count)
            throw new Exception("Geçersiz rol seçimi.");

        var user = new AppUser(
            tenantId: request.TenantId,
            userName: request.UserName.Trim(),
            email: request.Email.Trim(),
            passwordHash: _passwordHasher.Hash(request.Password)
        );

        _db.AppUsers.Add(user);

        foreach (var role in roles)
        {
            _db.AppUserRoles.Add(new AppUserRole { User = user, Role = role });
        }

        await _db.SaveChangesAsync();

        return user.Id;
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.AppUsers
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            throw new Exception("Kullanıcı bulunamadı.");

        if (!_currentUser.IsSystemTenant && user.TenantId != _currentUser.TenantId)
            throw new UnauthorizedAccessException();

        if (request.RoleIds.Count == 0)
            throw new Exception("Kullanıcının en az bir rolü olmalıdır.");

        user.SetUserName(request.UserName.Trim());
        user.SetEmail(request.Email.Trim());
        user.SetSecurityStamp();

        _db.AppUserRoles.RemoveRange(user.UserRoles);

        var roles = await _db.AppRoles
            .Where(x => x.TenantId == user.TenantId && request.RoleIds.Contains(x.Id))
            .ToListAsync();

        if (roles.Count != request.RoleIds.Count)
            throw new Exception("Geçersiz rol seçimi.");

        foreach (var role in roles)
        {
            _db.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return;

        if (!_currentUser.IsSystemTenant && user.TenantId != _currentUser.TenantId)
            throw new UnauthorizedAccessException();

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();
    }

    private async Task<AppRole> GetOrCreateRegisteredRoleAsync(Guid tenantId)
    {
        string normalized = Normalize(RegisteredRoleName);

        var role = await _db.AppRoles
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.NormalizedName == normalized);

        if (role is not null)
            return role;

        role = new AppRole(tenantId: tenantId, name: RegisteredRoleName);

        _db.AppRoles.Add(role);

        return role;
    }

    private async Task ValidatePasswordAsync(Guid tenantId, string password)
    {
        var policy = await _db.AuthPasswordPolicies
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        policy ??= new AuthPasswordPolicy { TenantId = tenantId };

        if (password.Length < policy.MinLength)
            throw new Exception($"Şifre en az {policy.MinLength} karakter olmalıdır.");

        if (policy.RequireDigit && !password.Any(char.IsDigit))
            throw new Exception("Şifre en az bir rakam içermelidir.");

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            throw new Exception("Şifre en az bir küçük harf içermelidir.");

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            throw new Exception("Şifre en az bir büyük harf içermelidir.");

        if (policy.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            throw new Exception("Şifre en az bir özel karakter içermelidir.");
    }

    private async Task IncreaseFailedLoginAsync(AppUser user)
    {
        var policy = await _db.AuthPasswordPolicies
            .FirstOrDefaultAsync(x => x.TenantId == user.TenantId);

        policy ??= new AuthPasswordPolicy { TenantId = user.TenantId };

        user.FailedLoginCount++;

        if (user.FailedLoginCount >= policy.MaxFailedLoginCount)
            user.LockoutEndAt = DateTime.UtcNow.AddMinutes(policy.LockoutMinutes);

        await _db.SaveChangesAsync();
    }

    private async Task AddLoginAuditAsync(
        Guid? tenantId,
        Guid? userId,
        string userNameOrEmail,
        bool success,
        string? failureReason,
        string? ipAddress,
        string? userAgent)
    {
        _db.AuthLoginAudits.Add(new AuthLoginAudit
        {
            TenantId = tenantId,
            UserId = userId,
            UserNameOrEmail = userNameOrEmail,
            IsSuccess = success,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        await Task.CompletedTask;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}