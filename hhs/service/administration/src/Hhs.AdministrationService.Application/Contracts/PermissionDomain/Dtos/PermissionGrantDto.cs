using HsnSoft.Base.Application.Dtos;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;

public sealed class PermissionGrantDto : EntityDto<Guid>
{
    public string Name { get; set; }

    public string ProviderName { get; set; }

    public string ProviderKey { get; set; }
}