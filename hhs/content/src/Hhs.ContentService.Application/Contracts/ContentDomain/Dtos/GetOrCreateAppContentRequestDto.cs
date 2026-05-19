using System.ComponentModel.DataAnnotations;
using Hhs.ContentService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;

public sealed class GetOrCreateAppContentRequestDto : IValidatableObject
{
    public Guid? CustomerId { get; set; }

    [CanBeNull] public string DomainName { get; set; }

    [CanBeNull] public string ContentKey { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple([
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]);

        if (!CustomerId.HasValue)
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.IsNotEmpty], [localizer?["Content:CustomerId"].ToString()]);
        }

        if (!CheckSafe.NotNullOrEmpty(DomainName, nameof(DomainName)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?["Content:DomainName"].ToString()]);
        }

        if (!CheckSafe.NotNullOrEmpty(ContentKey, nameof(ContentKey)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?["Content:ContentKey"].ToString()]);
        }
    }
}