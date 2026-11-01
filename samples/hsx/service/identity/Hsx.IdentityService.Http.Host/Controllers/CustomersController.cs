using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>The customers: every action only calls the application service, which holds the rules.</summary>
[Route("api/identity-service/v1/commercial/customers")]
public sealed class CustomersController(ICustomerAppService customerAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync([FromBody] GetCustomersPaged pagedInput, CancellationToken cancellationToken = default)
        => customerAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerDto created = await customerAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
