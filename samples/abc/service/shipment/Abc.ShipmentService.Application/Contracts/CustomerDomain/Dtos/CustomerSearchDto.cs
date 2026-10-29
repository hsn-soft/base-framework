using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos;

/// <summary>What a search shows of a customer: enough to recognize and pick one.</summary>
public sealed class CustomerSearchDto : EntityDto<Guid>
{
    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;
}
