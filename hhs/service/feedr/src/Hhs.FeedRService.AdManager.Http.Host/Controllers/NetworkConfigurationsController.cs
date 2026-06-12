using Hhs.FeedRService.AdManager.Controllers.Base;
using Hhs.FeedRService.Application.Contracts.CustomerDomain;
using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.AdManager.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("feedr-admanager-service")]
[ApiController]
[Route("api/[area]/v1/commercial/network-configurations")]
public sealed class NetworkConfigurationsController(
    IServiceProvider provider,
    INetworkConfigurationAppService networkConfigurationAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NetworkConfigurationDto), StatusCodes.Status200OK)]
    public async Task<NetworkConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => await networkConfigurationAppService.GetAsync(id, cancellationToken);

    [HttpGet("by-network-code/{networkCode}")]
    [ProducesResponseType(typeof(NetworkConfigurationDto), StatusCodes.Status200OK)]
    public async Task<NetworkConfigurationDto> GetByNetworkCodeAsync(string networkCode, CancellationToken cancellationToken)
        => await networkConfigurationAppService.GetByNetworkCodeAsync(networkCode, cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(List<NetworkConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<List<NetworkConfigurationDto>> GetAllActiveAsync(CancellationToken cancellationToken)
        => await networkConfigurationAppService.GetAllActiveAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(NetworkConfigurationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateNetworkConfigurationDto input, CancellationToken cancellationToken)
    {
        var result = await networkConfigurationAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NetworkConfigurationDto), StatusCodes.Status200OK)]
    public async Task<NetworkConfigurationDto> UpdateAsync(Guid id, [FromBody] UpdateNetworkConfigurationDto input, CancellationToken cancellationToken)
        => await networkConfigurationAppService.UpdateAsync(id, input, cancellationToken);
}
