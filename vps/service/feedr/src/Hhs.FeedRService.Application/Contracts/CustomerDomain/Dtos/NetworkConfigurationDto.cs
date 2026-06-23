namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class NetworkConfigurationDto
{
    public Guid Id { get; set; }
    public Guid NetworkId { get; set; }
    public Guid TenantId { get; set; }
    public string NetworkCode { get; set; }
    public string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public List<TopLevelGroupConfigDto> TopLevelGroups { get; set; }
}
