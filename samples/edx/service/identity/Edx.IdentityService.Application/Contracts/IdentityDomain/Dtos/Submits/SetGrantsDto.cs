using System.ComponentModel.DataAnnotations;

namespace Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;

/// <summary>What a role or a user is granted from now on: exactly these, nothing else (the ones it had are replaced).</summary>
public sealed class SetGrantsDto
{
    [Required]
    [MaxLength(1000)]
    public List<GrantInputDto> Grants { get; set; } = [];
}

public sealed class GrantInputDto
{
    /// <summary>The code of the permission (<c>endpoint.intake.files.get</c>).</summary>
    [Required]
    [StringLength(300)]
    public string Code { get; set; } = string.Empty;

    /// <summary>The value of a constraint (<c>20971520</c>); it must be given for a constraint and left out for any other permission.</summary>
    [StringLength(256)]
    public string? Value { get; set; }
}
