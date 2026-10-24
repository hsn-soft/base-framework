using Abc.IdentityService.Domain.UserDomain.Consts;
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

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string userName, string email, string displayName, DateTimeOffset now)
    {
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = Required(userName, UserConsts.UserNameMaxLength, nameof(UserName)),
            IsActive = true,
            CreatedAt = now,
        };
        user.NormalizedUserName = Normalize(user.UserName);
        user.Change(email, displayName, now);
        return user;
    }

    public static string Normalize(string userName) => userName.Trim().ToUpperInvariant();

    public void Update(string email, string displayName, bool isActive, DateTimeOffset now)
    {
        Change(email, displayName, now);
        IsActive = isActive;
    }

    private void Change(string email, string displayName, DateTimeOffset now)
    {
        Email = Required(email, UserConsts.EmailMaxLength, nameof(Email));
        DisplayName = Required(displayName, UserConsts.DisplayNameMaxLength, nameof(DisplayName));
        UpdatedAt = now;
    }

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.")
            : text;
    }
}
