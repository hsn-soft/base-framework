using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Security;
using HsnSoft.Base;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Abc.IdentityService.Domain.UserDomain.Entities;

/// <summary>
/// A person who can sign in to the Abc system. The audit fields, the soft delete and the tenant are done by the persistence (<see cref="ICreationAudited"/>, <see cref="IModificationAudited"/>,
/// <see cref="ISoftDelete"/>, <see cref="IMultiTenant"/>): the entity only keeps them.
/// </summary>
public sealed class User : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
{
    // for EF Core
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    /// <summary>The user name in capitals: what is unique, so "Ada" and "ada" are one name.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>The email in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>The language the user is answered in (<c>en</c>, <c>tr</c>, <c>ru</c>): the <c>locale</c> claim of the token. A language that is not supported is not an error; the request's own header decides then.</summary>
    public string Language { get; private set; } = UserConsts.DefaultLanguage;

    /// <summary>What the password was made into by <see cref="PasswordHasher"/>; the password itself is never kept.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>The roles of the user, in small letters (<see cref="UserRoles"/>): they become the <c>role</c> claims of the token.</summary>
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public bool IsActive { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

    public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

    public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);

    public static User Create(string userName, string email, string displayName, string password, IEnumerable<string>? roles, string? language = null)
    {
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), UserConsts.UserNameMaxLength),
            IsActive = true,
            PasswordHash = PasswordHasher.Hash(Guard.Length(password, nameof(password), UserConsts.PasswordMaxLength, UserConsts.PasswordMinLength)!),
        };
        user.NormalizedUserName = Normalize(user.UserName);
        user.Change(email, displayName, roles, language);
        return user;
    }

    /// <summary>Whether <paramref name="password"/> is the user's.</summary>
    public bool CheckPassword(string password) => PasswordHasher.Verify(PasswordHash, password);

    /// <summary>A user name or an email as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    public void Update(string email, string displayName, bool isActive, IEnumerable<string>? roles, string? language = null)
    {
        Change(email, displayName, roles, language);
        IsActive = isActive;
    }

    private void Change(string email, string displayName, IEnumerable<string>? roles, string? language)
    {
        Language = string.IsNullOrWhiteSpace(language) ? UserConsts.DefaultLanguage : Guard.NotNullOrWhiteSpace(language, nameof(Language), UserConsts.LanguageMaxLength).ToLowerInvariant();
        Roles = NormalizeRoles(roles);
        Email = Guard.NotNullOrWhiteSpace(email, nameof(Email), UserConsts.EmailMaxLength);
        NormalizedEmail = Normalize(Email);
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(DisplayName), UserConsts.DisplayNameMaxLength);
    }

    private static string[] NormalizeRoles(IEnumerable<string>? roles)
    {
        string[] normalized = [.. (roles ?? [UserRoles.Customer]).Select(role => role?.Trim().ToLowerInvariant() ?? string.Empty).Distinct()];
        return normalized.Any(role => role.Length == 0 || role.Length > UserConsts.RoleMaxLength || role.Contains(','))
            ? throw new DomainException($"A role is at least one and at most {UserConsts.RoleMaxLength} characters, without a comma.").WithKey("role_invalid", UserConsts.RoleMaxLength)
            : normalized;
    }
}
