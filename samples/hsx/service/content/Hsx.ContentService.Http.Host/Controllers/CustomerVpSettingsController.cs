using HsnSoft.Base.Application.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Services;
using Hsx.ContentService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Http.Host.Controllers;

/// <summary>The customer vp settings: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/customer-vp-settings")]
public sealed class CustomerVpSettingsController(ICustomerVpSettingAppService customerVpSettingAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerVpSettingAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerVpSettingDto>> GetPagedListAsync([FromBody] GetCustomerVpSettingsPaged pagedInput, CancellationToken cancellationToken = default)
        => customerVpSettingAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerVpSettingCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerVpSettingDto created = await customerVpSettingAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CustomerVpSettingUpdateDto input, CancellationToken cancellationToken = default)
    {
        await customerVpSettingAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerVpSettingAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
