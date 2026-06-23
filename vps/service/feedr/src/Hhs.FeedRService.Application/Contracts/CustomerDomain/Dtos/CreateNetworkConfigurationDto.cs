using System.ComponentModel.DataAnnotations;

namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class CreateNetworkConfigurationDto
{
    [Required] public Guid TenantId { get; set; }
    [Required] public string NetworkCode { get; set; }
    [Required] public string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TopLevelGroupConfigDto> TopLevelGroups { get; set; }
}
