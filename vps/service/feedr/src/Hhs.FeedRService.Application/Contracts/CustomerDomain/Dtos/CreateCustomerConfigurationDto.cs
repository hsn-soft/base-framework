using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CreateCustomerConfigurationDto
{
    [Required] public Guid TenantId { get; set; }
    [Required] public Guid ClientId { get; set; }
    [Required] public string ClientName { get; set; }
    [Required] public string Network { get; set; }
    public string AdUnitName { get; set; }
    public string AdUnitIdTopLevel { get; set; }
    public List<string> AdUnitId { get; set; }
}
