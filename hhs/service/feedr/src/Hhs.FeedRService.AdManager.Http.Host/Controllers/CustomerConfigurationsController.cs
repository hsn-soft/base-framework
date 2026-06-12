using Hhs.FeedRService.AdManager.Controllers.Base;
using Hhs.FeedRService.Application.Contracts.CustomerDomain;
using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.AdManager.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("feedr-admanager-service")]
[ApiController]
[Route("api/[area]/v1/commercial/customer-configurations")]
public sealed class CustomerConfigurationsController( IServiceProvider provider,
    ICustomerConfigurationAppService customerConfigurationAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerConfigurationDto), StatusCodes.Status200OK)]
    public async Task<CustomerConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken)
        => await customerConfigurationAppService.GetAsync(id, cancellationToken);

    [HttpGet("by-client/{clientId:guid}")]
    [ProducesResponseType(typeof(CustomerConfigurationDto), StatusCodes.Status200OK)]
    public async Task<CustomerConfigurationDto> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken)
        => await customerConfigurationAppService.GetByClientIdAsync(clientId, cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(List<CustomerConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<List<CustomerConfigurationDto>> GetAllAsync(CancellationToken cancellationToken)
        => await customerConfigurationAppService.GetAllAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(CustomerConfigurationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCustomerConfigurationDto input, CancellationToken cancellationToken)
    {
        var result = await customerConfigurationAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerConfigurationDto), StatusCodes.Status200OK)]
    public async Task<CustomerConfigurationDto> UpdateAsync(Guid id, [FromBody] UpdateCustomerConfigurationDto input, CancellationToken cancellationToken)
        => await customerConfigurationAppService.UpdateAsync(id, input, cancellationToken);
}
