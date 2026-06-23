namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class UpdateCustomerConfigurationDto
{
    public string ClientName { get; set; }
    public string Network { get; set; }
    public string AdUnitName { get; set; }
    public string AdUnitIdTopLevel { get; set; }
    public List<string> AdUnitId { get; set; }
}
