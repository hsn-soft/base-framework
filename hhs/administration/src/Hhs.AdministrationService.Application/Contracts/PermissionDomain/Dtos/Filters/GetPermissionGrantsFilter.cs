using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetPermissionGrantsFilter : SortedAndLimitedDataRequestDto
{
    [CanBeNull]
    public string Name { get; set; } = null;

    [CanBeNull]
    public string ProviderName { get; set; } = null;

    [CanBeNull]
    public string ProviderKey { get; set; } = null;
}