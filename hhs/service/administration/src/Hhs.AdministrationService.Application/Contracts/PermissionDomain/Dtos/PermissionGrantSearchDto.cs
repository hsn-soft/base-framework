using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;

public sealed class PermissionGrantSearchDto : EntityDto<Guid>
{
    [NotNull]
    public string Name { get; set; }
}