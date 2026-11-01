using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>The companies: every action only calls the application service, which holds the rules.</summary>
[Route("api/identity-service/v1/commercial/companies")]
public sealed class CompaniesController(ICompanyAppService companyAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CompanyDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => companyAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CompanyDto>> GetPagedListAsync([FromBody] GetCompaniesPaged pagedInput, CancellationToken cancellationToken = default)
        => companyAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CompanyCreateDto input, CancellationToken cancellationToken = default)
    {
        CompanyDto created = await companyAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CompanyUpdateDto input, CancellationToken cancellationToken = default)
    {
        await companyAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await companyAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
