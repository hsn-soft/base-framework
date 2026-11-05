using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Security;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserDomain.Entities;

/// <summary>
/// A person who signs in, in one tenant (its email and its user name are unique in the tenant, so a sign-in names the tenant). The tenant is kept here and given by the persistence (the tenant of the caller who makes the user). The audit fields come from
/// <see cref="AuditedEntity{TKey}"/>; it is deleted softly (<see cref="ISoftDelete"/>).
/// </summary>
public sealed class AppUser : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    // for EF Core
    private AppUser()
    {
    }

    public bool IsDeleted { get; private set; }

    public Guid TenantId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? LanguageCode { get; private set; }

    /// <summary>A user the system made, which is not deleted.</summary>
    public bool IsStatic { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool PhoneNumberConfirmed { get; private set; }

    /// <summary>Where the picture of the user is, after the address of the file store.</summary>
    public string? AvatarSuffixUrl { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>How many sign-ins failed since the last one that worked; the persistence counts them in one command (<see cref="Repositories.IAppUserRepository"/>), so two failures at once are both counted.</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>Until when the user is locked out after too many failed sign-ins; nothing when it is not.</summary>
    public DateTimeOffset? LockoutEndAt { get; private set; }

    /// <summary>
    /// Until when the password is a temporary one (the system made it, no person knows it, the user was sent it); nothing when the user chose its own. A temporary password is good for one thing: to sign in and change it. When the user sets its own
    /// password (<see cref="SetPassword"/>) the temporary one is gone at once.
    /// </summary>
    public DateTimeOffset? TemporaryPasswordExpiresAt { get; private set; }

    /// <summary>Whether the password is a temporary one that the user has to change.</summary>
    public bool HasTemporaryPassword => TemporaryPasswordExpiresAt is not null;

    /// <summary>Whether the password is temporary and its time is over at <paramref name="now"/>: it signs in no more, and the user needs a new one.</summary>
    public bool IsTemporaryPasswordExpired(DateTimeOffset now) => TemporaryPasswordExpiresAt is { } end && end <= now;

    /// <summary>Whether the user is locked out at <paramref name="now"/>.</summary>
    public bool IsLockedOut(DateTimeOffset now) => LockoutEndAt is { } end && end > now;

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public Tenant? Tenant { get; }
    public ICollection<AppUserRole> UserRoles { get; } = [];

    public static AppUser Create(string userName, string email, string displayName, string password, string? languageCode, string? phoneNumber = null, string? avatarSuffixUrl = null, bool isStatic = false, Guid? id = null)
    {
        AppUser user = new()
        {
            Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), AppUserConsts.UserNameMaxLength).Trim(),
            IsActive = true,
            IsStatic = isStatic,
        };
        user.NormalizedUserName = Guard.NotNullOrWhiteSpace(StringHelper.Normalize(user.UserName), nameof(NormalizedUserName), AppUserConsts.UserNameMaxLength);
        user.Update(email, displayName, languageCode, true, phoneNumber, avatarSuffixUrl);
        user.SetPassword(password);
        return user;
    }

    /// <summary>A user whose first password is a temporary one: the system made it and sent it, and it is good until <paramref name="expiresAt"/>.</summary>
    public static AppUser CreateWithTemporaryPassword(string userName, string email, string displayName, string temporaryPassword, DateTimeOffset expiresAt, string? languageCode, string? phoneNumber = null, string? avatarSuffixUrl = null, bool isStatic = false, Guid? id = null)
    {
        AppUser user = Create(userName, email, displayName, temporaryPassword, languageCode, phoneNumber, avatarSuffixUrl, isStatic, id);
        user.TemporaryPasswordExpiresAt = expiresAt;
        return user;
    }

    public void Update(string email, string displayName, string? languageCode, bool isActive, string? phoneNumber = null, string? avatarSuffixUrl = null)
    {
        Email = Guard.NotNullOrWhiteSpace(email, nameof(Email), AppUserConsts.EmailMaxLength).Trim();
        NormalizedEmail = Guard.NotNullOrWhiteSpace(StringHelper.Normalize(Email), nameof(NormalizedEmail), AppUserConsts.EmailMaxLength);
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(DisplayName), AppUserConsts.DisplayNameMaxLength).Trim();
        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? null : Guard.Length(languageCode.Trim().ToLowerInvariant(), nameof(LanguageCode), AppUserConsts.LanguageCodeMaxLength);
        PhoneNumber = Optional(phoneNumber, nameof(PhoneNumber), AppUserConsts.PhoneNumberMaxLength);
        AvatarSuffixUrl = Optional(avatarSuffixUrl, nameof(AvatarSuffixUrl), AppUserConsts.AvatarSuffixUrlMaxLength);
        IsActive = isActive;
    }

    /// <summary>The email was proved to be the user's.</summary>
    public void ConfirmEmail() => EmailConfirmed = true;

    /// <summary>The phone number was proved to be the user's.</summary>
    public void ConfirmPhoneNumber() => PhoneNumberConfirmed = PhoneNumber is not null ? true : throw new InvalidOperationException("The user has no phone number to confirm.");

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);

    /// <summary>Sets the password the user chose (or the administrator set): the old one, a temporary one too, stops working at once. A user with a temporary password cannot keep it: the new one has to be another.</summary>
    public void SetPassword(string password)
    {
        string text = Guard.NotNullOrWhiteSpace(password, "password", AppUserConsts.PasswordMaxLength, AppUserConsts.PasswordMinLength);
        if (HasTemporaryPassword && PasswordHasher.Verify(PasswordHash, text))
        {
            throw new AppUserPasswordNotNewException();
        }

        PasswordHash = PasswordHasher.Hash(text);
        TemporaryPasswordExpiresAt = null;
    }

    /// <summary>The administrator asks for a new first password: the user gets a temporary one again, the old password stops working, and a lockout of too many failed sign-ins is lifted (the user lost its password, it is not an attack).</summary>
    public void SetTemporaryPassword(string temporaryPassword, DateTimeOffset expiresAt)
    {
        FailedLoginCount = 0;
        LockoutEndAt = null;
        PasswordHash = PasswordHasher.Hash(Guard.NotNullOrWhiteSpace(temporaryPassword, "password", AppUserConsts.PasswordMaxLength, AppUserConsts.PasswordMinLength));
        TemporaryPasswordExpiresAt = expiresAt;
    }
}
