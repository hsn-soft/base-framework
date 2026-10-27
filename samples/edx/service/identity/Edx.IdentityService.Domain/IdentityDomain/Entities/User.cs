using Edx.IdentityService.Domain.IdentityDomain.Consts;
using Edx.IdentityService.Domain.IdentityDomain.Security;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A person who can sign in to the Edx system, in one tenant: the user name only has to be unique in it (every tenant may have an <c>admin</c>). The tenant is kept here and given by the persistence.</summary>
public sealed class User : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private User()
    {
    }

    public string UserName { get; private set; } = string.Empty;

    /// <summary>The user name in capitals: what is unique in a tenant, so "Admin" and "admin" are one name.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>The email in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>What the password was made into by <see cref="PasswordHasher"/>; the password itself is never kept.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public Guid TenantId { get; private set; }

    public static User Create(string userName, string email, string displayName, string password)
    {
        User user = new()
        {
            Id = Guid.CreateVersion7(),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), IdentityConsts.UserNameMaxLength),
            Email = Guard.NotNullOrWhiteSpace(email, nameof(Email), IdentityConsts.EmailMaxLength),
            DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(DisplayName), IdentityConsts.NameMaxLength),
            PasswordHash = PasswordHasher.Hash(Guard.Length(password, nameof(password), IdentityConsts.PasswordMaxLength, IdentityConsts.PasswordMinLength)!),
            IsActive = true,
        };
        user.NormalizedUserName = StringHelper.Normalize(user.UserName);
        user.NormalizedEmail = StringHelper.Normalize(user.Email);
        return user;
    }
}
