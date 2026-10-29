using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CustomerDto : AuditedEntityDto<Guid>
{
    public Guid? UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Address { get; init; }
}
