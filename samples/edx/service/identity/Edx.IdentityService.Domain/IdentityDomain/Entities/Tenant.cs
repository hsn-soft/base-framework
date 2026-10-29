using Edx.IdentityService.Domain.IdentityDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>
/// A company that uses the system, or the system itself. It is not <c>IMultiTenant</c>: it is what the tenants are, so there is no tenant filter on it; the application service lets a user of a customer see only its own
/// row, and the users of the system tenant see them all. The id is chosen by the caller (the ids of the sample are constants of <c>Edx.Shared.Helper</c>).
/// </summary>
public sealed class Tenant : Entity<Guid>
{
    // for EF Core
    private Tenant()
    {
    }

    /// <summary>What a user names its tenant by when it signs in (<c>veriban</c>): in small letters, unique.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Tenant Create(Guid id, string code, string name)
        => new()
        {
            Id = Guard.NotDefault<Guid>(id, nameof(Id)),
            Code = StringHelper.Normalize(Guard.NotNullOrWhiteSpace(code, nameof(Code), IdentityConsts.CodeMaxLength)).ToLowerInvariant(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), IdentityConsts.NameMaxLength),
            IsActive = true,
        };
}
