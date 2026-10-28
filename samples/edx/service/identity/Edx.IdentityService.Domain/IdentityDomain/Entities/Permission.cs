using System.Text.RegularExpressions;
using Edx.IdentityService.Domain.IdentityDomain.Consts;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>
/// A thing the system can let someone do, named by a code the code of the services asks for (<c>endpoint.invoice.read</c>). The permissions are the catalogue of the system: the constants of <c>Edx.Shared.Helper</c> are the only list, the identity service keeps it in
/// step at every start, and what is granted to a role is a link to one of these. A permission is not <c>IMultiTenant</c>: the catalogue is the system's, whatever tenant asks. A code that is not in the code any more is not deleted (the grants that name it would be)
/// but <see cref="IsActive"/> is <see langword="false"/>, and nothing is granted by it.
/// </summary>
public sealed partial class Permission : Entity<Guid>
{
    // for EF Core
    private Permission()
    {
    }

    /// <summary>The code: <c>{type}.{resource}.{action}</c> (three parts at least), in small letters, digits and dashes, the type being the first part. Unique.</summary>
    public string Code { get; private set; } = string.Empty;

    public PermissionType Type { get; private set; }

    /// <summary>What a person reads in a list of permissions (the code, unless a name was given).</summary>
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public static Permission Create(string code, string? name = null, string? description = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Code = Valid(code),
            Type = TypeOf(code),
            Name = string.IsNullOrWhiteSpace(name) ? code : Guard.Length(name.Trim(), nameof(Name), IdentityConsts.NameMaxLength)!,
            Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Length(description.Trim(), nameof(Description), IdentityConsts.PermissionDescriptionMaxLength),
            IsActive = true,
        };

    /// <summary>The type a code names with its first part.</summary>
    public static PermissionType TypeOf(string code)
        => Valid(code).Split('.')[0] switch
        {
            "page" => PermissionType.Page,
            "endpoint" => PermissionType.Endpoint,
            "operation" => PermissionType.Operation,
            _ => PermissionType.Constraint,
        };

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>The value a grant of this permission carries: a constraint must have one, any other permission must not.</summary>
    public string? CheckValue(string? value)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return Type == PermissionType.Constraint
            ? text is null ? throw new PermissionValueException(PermissionValueException.Required, Code) : Guard.Length(text, nameof(value), IdentityConsts.PermissionValueMaxLength)
            : text is null ? null : throw new PermissionValueException(PermissionValueException.NotAllowed, Code);
    }

    private static string Valid(string code)
    {
        string text = Guard.NotNullOrWhiteSpace(code, nameof(Code), IdentityConsts.PermissionCodeMaxLength);
        return CodePattern().IsMatch(text) ? text : throw new DomainException($"'{text}' is not a permission code: {{page|endpoint|operation|constraint}}.{{resource}}.{{action}} in small letters, digits and dashes.").WithKey("permission_code_invalid", text);
    }

    [GeneratedRegex(@"^(page|endpoint|operation|constraint)(\.[a-z0-9]+(-[a-z0-9]+)*){2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
