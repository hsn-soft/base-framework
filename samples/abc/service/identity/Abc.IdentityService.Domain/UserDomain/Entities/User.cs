using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Security;
using HsnSoft.Base;

namespace Abc.IdentityService.Domain.UserDomain.Entities;

/// <summary>A person who can sign in to the Abc system.</summary>
public sealed class User
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

    /// <summary>What the password was made into by <see cref="PasswordHasher"/>; the password itself is never kept.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>The roles of the user, in small letters (<see cref="UserRoles"/>): they become the <c>role</c> claims of the token.</summary>
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The user name in the <c>sub</c> claim of the token of who made the user; <see langword="null"/> when nobody signed in did.</summary>
    public string? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The same for the last change.</summary>
    public string? UpdatedBy { get; private set; }

    public static User Create(string userName, string email, string displayName, string password, IEnumerable<string>? roles, DateTimeOffset now, string? by)
    {
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = Required(userName, UserConsts.UserNameMaxLength, nameof(UserName)),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = Author(by),
            PasswordHash = PasswordHasher.Hash(ValidPassword(password)),
        };
        user.NormalizedUserName = Normalize(user.UserName);
        user.Change(email, displayName, roles, now, by);
        return user;
    }

    /// <summary>Whether <paramref name="password"/> is the user's.</summary>
    public bool CheckPassword(string password) => PasswordHasher.Verify(PasswordHash, password);

    /// <summary>A user name or an email as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    public void Update(string email, string displayName, bool isActive, IEnumerable<string>? roles, DateTimeOffset now, string? by)
    {
        Change(email, displayName, roles, now, by);
        IsActive = isActive;
    }

    private void Change(string email, string displayName, IEnumerable<string>? roles, DateTimeOffset now, string? by)
    {
        Roles = NormalizeRoles(roles);
        UpdatedBy = Author(by);
        Email = Required(email, UserConsts.EmailMaxLength, nameof(Email));
        NormalizedEmail = Normalize(Email);
        DisplayName = Required(displayName, UserConsts.DisplayNameMaxLength, nameof(DisplayName));
        UpdatedAt = now;
    }

    private static string[] NormalizeRoles(IEnumerable<string>? roles)
    {
        string[] normalized = [.. (roles ?? [UserRoles.Customer]).Select(role => role?.Trim().ToLowerInvariant() ?? string.Empty).Distinct()];
        return normalized.Any(role => role.Length == 0 || role.Length > UserConsts.RoleMaxLength || role.Contains(','))
            ? throw new DomainException($"A role is at least one and at most {UserConsts.RoleMaxLength} characters, without a comma.")
            : normalized;
    }

    private static string ValidPassword(string password)
        => password is null || password.Length < UserConsts.PasswordMinLength || password.Length > UserConsts.PasswordMaxLength
            ? throw new DomainException($"The password is {UserConsts.PasswordMinLength} to {UserConsts.PasswordMaxLength} characters.")
            : password;

    private static string? Author(string? by) => string.IsNullOrWhiteSpace(by) ? null : by.Trim()[..Math.Min(by.Trim().Length, UserConsts.CreatedByMaxLength)];

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.")
            : text;
    }
}
