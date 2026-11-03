using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Services;
using Hsx.ContentService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.ContentService.Http.Host.Controllers;

/// <summary>The customer vp settings: every action only calls the application service, which holds the rules.</summary>
[Route("api/content-service/v1/commercial/customer-content-vp-settings")]
public sealed class CustomerContentVpSettingsController(ICustomerContentVpSettingAppService customerContentVpSettingAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVpSettings.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerContentVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerContentVpSettingAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVpSettings.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerContentVpSettingDto>> GetPagedListAsync([FromBody] GetCustomerContentVpSettingsPaged pagedInput, CancellationToken cancellationToken = default)
        => customerContentVpSettingAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVpSettings.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerContentVpSettingCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerContentVpSettingDto created = await customerContentVpSettingAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVpSettings.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CustomerContentVpSettingUpdateDto input, CancellationToken cancellationToken = default)
    {
        await customerContentVpSettingAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Content.CustomerContentVpSettings.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerContentVpSettingAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
