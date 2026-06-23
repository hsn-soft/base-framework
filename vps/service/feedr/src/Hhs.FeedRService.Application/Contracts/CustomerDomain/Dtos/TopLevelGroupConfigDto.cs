namespace Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

public sealed class TopLevelGroupConfigDto
{
    public string AdUnitTopLevelCode { get; set; }
    public string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
}
