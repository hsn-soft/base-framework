using System.Globalization;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AppUser : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    [CanBeNull] public Tenant Tenant { get; set; }

    public bool IsStatic { get; set; } // can't delete

    [CanBeNull] public string DisplayName { get; private set; }

    [CanBeNull] public string AvatarSuffixUrl { get; set; }

    [NotNull] public string UserName { get; private set; }
    [NotNull] public string NormalizedUserName { get; private set; }

    [NotNull] public string Email { get; private set; }
    [NotNull] public string NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; set; }

    [CanBeNull] public string PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; set; }

    [NotNull] public string PasswordHash { get; private set; }

    [NotNull] public string SecurityStamp { get; private set; } // user password change check

    [CanBeNull] public string LanguageCode { get; private set; }

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<AppUserRole> UserRoles { get; set; }
    public ICollection<AppUserClaim> Claims { get; set; }
    public ICollection<AuthRefreshToken> RefreshTokens { get; set; }

    private AppUser()
    {
        // Not-Null string fields
        UserName = string.Empty;
        NormalizedUserName = string.Empty;
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;

        // include arrays
        UserRoles = [];
        Claims = [];
        RefreshTokens = [];
    }

    internal AppUser(Guid tenantId,
        [NotNull] string userName,
        [NotNull] string email,
        [NotNull] string passwordHash,
        bool isStatic = false,
        [CanBeNull] string displayName = null,
        [CanBeNull] string avatarSuffixUrl = null,
        [CanBeNull] string phoneNumber = null,
        [CanBeNull] string languageCode = null
    ) : this(Guid.CreateVersion7(), tenantId, userName, email, passwordHash, isStatic, displayName, avatarSuffixUrl, phoneNumber, languageCode)
    {
    }

    internal AppUser(Guid id, Guid tenantId,
        [NotNull] string userName,
        [NotNull] string email,
        [NotNull] string passwordHash,
        bool isStatic = false,
        [CanBeNull] string displayName = null,
        [CanBeNull] string avatarSuffixUrl = null,
        [CanBeNull] string phoneNumber = null,
        [CanBeNull] string languageCode = null
    ) : this()
    {
        Id = id;
        TenantId = tenantId;

        IsStatic = isStatic;

        SetDisplayName(displayName);
        SetAvatarSuffixUrl(avatarSuffixUrl);

        SetUserName(userName);
        SetEmail(email);
        SetPhoneNumber(phoneNumber);

        SetPasswordHash(passwordHash);
        SetSecurityStamp();

        SetLanguageCode(languageCode);
    }

    internal void SetDisplayName(string displayName) =>
        DisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(displayName, $"{nameof(AppUser)}:{nameof(DisplayName)}", AppUserConsts.DisplayNameMaxLength)
            : null;

    internal void SetAvatarSuffixUrl(string avatarSuffixUrl) =>
        AvatarSuffixUrl = !string.IsNullOrWhiteSpace(avatarSuffixUrl)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(avatarSuffixUrl, $"{nameof(AppUser)}:{nameof(AvatarSuffixUrl)}", AppUserConsts.AvatarSuffixUrlMaxLength)
            : null;

    internal void SetUserName(string userName)
    {
        string checkUserName = LocalizedModelValidator.NotNullOrWhiteSpace(userName, $"{nameof(AppRole)}:{nameof(UserName)}", AppUserConsts.UserNameMaxLength);
        UserName = StringOperations.Minimize(checkUserName);
        NormalizedUserName = StringOperations.Normalize(UserName);
    }

    internal void SetEmail(string email)
    {
        string checkEmail = LocalizedModelValidator.NotNullOrWhiteSpace(email, $"{nameof(AppRole)}:{nameof(Email)}", AppUserConsts.EmailMaxLength);
        Email = StringOperations.Minimize(checkEmail);
        NormalizedEmail = StringOperations.Normalize(Email);
    }

    internal void SetPhoneNumber(string phoneNumber) =>
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(phoneNumber, $"{nameof(AppUser)}:{nameof(PhoneNumber)}", AppUserConsts.PhoneNumberMaxLength)
            : null;

    internal void SetPasswordHash(string passwordHash) =>
        PasswordHash = LocalizedModelValidator.NotNullOrWhiteSpace(passwordHash, $"{nameof(AppUser)}:{nameof(PasswordHash)}", AppUserConsts.PasswordHashMaxLength);

    internal void SetSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");

    internal void SetLanguageCode(string defaultLanguage)
    {
        if (!string.IsNullOrWhiteSpace(defaultLanguage))
        {
            LanguageCode = LocalizedModelValidator.Length(defaultLanguage, $"{nameof(AppUser)}:{nameof(LanguageCode)}", AppUserConsts.LanguageCodeMaxLength);

            string[] acceptableLanguages = ["en", "ru", "tr"];
            if (!acceptableLanguages.Contains((LanguageCode ?? "").ToLower(new CultureInfo("en-US"))))
            {
                LanguageCode = null;
            }
        }
        else
        {
            LanguageCode = null;
        }
    }
}