using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.AppUserDomain.Security;

namespace Hsx.IdentityService.Domain.AppUserDomain.Entities;

/// <summary>
/// A person who signs in, in one tenant (its email is unique in the system: it is what signs in). The tenant is kept here and given by the persistence (the tenant of the caller who makes the user). The audit fields and the soft delete come from
/// <see cref="FullAuditedEntity{TKey}"/>.
/// </summary>
public sealed class AppUser : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private AppUser()
    {
    }

    public Guid TenantId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? LanguageCode { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public static AppUser Create(string userName, string email, string displayName, string password, string? languageCode)
    {
        AppUser user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), AppUserConsts.UserNameMaxLength).Trim(),
            IsActive = true,
        };
        user.NormalizedUserName = StringHelper.Normalize(user.UserName);
        user.Update(email, displayName, languageCode, true);
        user.SetPassword(password);
        return user;
    }

    public void Update(string email, string displayName, string? languageCode, bool isActive)
    {
        Email = Guard.NotNullOrWhiteSpace(email, nameof(Email), AppUserConsts.EmailMaxLength).Trim();
        NormalizedEmail = StringHelper.Normalize(Email);
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(DisplayName), AppUserConsts.DisplayNameMaxLength).Trim();
        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? null : Guard.Length(languageCode.Trim().ToLowerInvariant(), nameof(LanguageCode), AppUserConsts.LanguageCodeMaxLength);
        IsActive = isActive;
    }

    public void SetPassword(string password)
        => PasswordHash = PasswordHasher.Hash(Guard.NotNullOrWhiteSpace(password, "password", AppUserConsts.PasswordMaxLength, AppUserConsts.PasswordMinLength));
}
