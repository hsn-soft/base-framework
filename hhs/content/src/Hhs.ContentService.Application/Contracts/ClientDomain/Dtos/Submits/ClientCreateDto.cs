using System.ComponentModel.DataAnnotations;
using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Submits;

public sealed class ClientCreateDto : IValidatableObject
{
    public Guid? TenantId { get; set; }

    [CanBeNull]
    public string DomainName { get; set; }

    public bool IsBlocked { get; set; }

    public List<string> IncludePathFilters { get; set; }
    public List<string> ExcludePathFilters { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple([
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]);

        if (!TenantId.HasValue)
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.IsNotEmpty], [localizer?["TenantId"].ToString()]);
        }

        if (!CheckSafe.NotNullOrEmpty(DomainName, nameof(DomainName)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?["Client:DomainName"].ToString()]);
        }
        else if (!CheckSafe.Length(DomainName, nameof(DomainName), ClientConsts.DomainNameMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength], [localizer?["Client:DomainName"].ToString()]);
        }

        if (IncludePathFilters is { Count: > 0 })
        {
            foreach (string pathFilter in IncludePathFilters)
            {
                if (!CheckSafe.NotNullOrEmpty(pathFilter, nameof(pathFilter)))
                {
                    yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?["ClientPathFilter:PathFilterName"].ToString()]);
                }
                else if (!CheckSafe.Length(pathFilter, nameof(pathFilter), ClientPathFilterConsts.PathFilterNameMaxLength))
                {
                    yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength], [localizer?["ClientPathFilter:PathFilterName"].ToString()]);
                }
            }
        }

        if (ExcludePathFilters is { Count: > 0 })
        {
            foreach (string pathFilter in ExcludePathFilters)
            {
                if (!CheckSafe.NotNullOrEmpty(pathFilter, nameof(pathFilter)))
                {
                    yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?["ClientPathFilter:PathFilterName"].ToString()]);
                }
                else if (!CheckSafe.Length(pathFilter, nameof(pathFilter), ClientPathFilterConsts.PathFilterNameMaxLength))
                {
                    yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength], [localizer?["ClientPathFilter:PathFilterName"].ToString()]);
                }
            }
        }
    }
}