using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;

public enum SampleKind
{
    Standard = 1,
    Premium = 2,
}

public sealed class SampleAddressDto
{
    [Required]
    [StringLength(100)]
    public string? City { get; set; }
}

/// <summary>
/// A model of an update DTO that uses every rule of the framework (ADR-0044). It is not used by the service: copy the parts you need.
/// Every message the client gets is translated (en, tr, ru) and listed in <c>statusMessages</c>, one for each wrong field.
/// </summary>
public sealed class SampleUpdateDto : IValidatableObject
{
    /// <summary>The id: nullable so a missing one is told (<c>[Required]</c>), and <c>[NotDefault]</c> refuses <c>Guid.Empty</c>.</summary>
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>A text that must be sent: <c>[Required]</c> also refuses <c>""</c> and white space; the limit is told with its number.</summary>
    [Required]
    [StringLength(64)]
    public string? Name { get; set; }

    /// <summary>An optional text: when it is given it must not be blank (<c>[NotWhiteSpace]</c>); not given is fine.</summary>
    [NotWhiteSpace]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [RegularExpression("^[A-Z0-9-]{3,20}$")]
    public string? Sku { get; set; }

    /// <summary>A number that must be sent: nullable with <c>[Required]</c>, so a missing one is not <c>0</c>; <c>"abc"</c> or <c>1.5</c> for an int is an invalid value without any attribute.</summary>
    [Required]
    [Range(0, 1_000_000)]
    public decimal? Price { get; set; }

    [Required]
    [Range(1, 10_000)]
    public int? Quantity { get; set; }

    /// <summary>A bool that must be sent: nullable with <c>[Required]</c>, so a missing one is not <c>false</c>.</summary>
    [Required]
    public bool? IsActive { get; set; }

    /// <summary>An enum: nullable with <c>[Required]</c>; <c>[EnumDataType]</c> refuses a number that is not defined (a name that is not defined is refused by the JSON converter).</summary>
    [Required]
    [EnumDataType(typeof(SampleKind))]
    public SampleKind? Kind { get; set; }

    /// <summary>A text that must repeat another (<c>[Compare]</c>).</summary>
    [Compare(nameof(Email))]
    public string? EmailAgain { get; set; }

    /// <summary>An object: <c>[Required]</c> is needed to refuse <c>null</c> (a non-nullable type is not required by itself); the fields inside are checked too, as <c>Address.City</c>.</summary>
    [Required]
    public SampleAddressDto? Address { get; set; }

    /// <summary>A list that must have at least one item (<c>[MinLength]</c>) and no blank item (<c>[NoBlankItems]</c>).</summary>
    [MinLength(1)]
    [NoBlankItems]
    public List<string>? Tags { get; set; }

    /// <summary>A rule that depends on other fields: it runs only when every field above is valid. The text is a key of the localization files, with its arguments.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Kind == SampleKind.Premium && Price < 100)
        {
            yield return ValidationRule.Failed(nameof(Price), "Validation:Range", nameof(Price), 100, 1_000_000);
        }
    }
}
