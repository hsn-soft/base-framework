namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class UpdateNetworkConfigurationDto
{
    public string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public List<TopLevelGroupConfigDto> TopLevelGroups { get; set; }
}
