using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/companies")]
public sealed class CompaniesController(ICompanyAppService companies) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Companies.Read)]
    public Task<CompanyDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => companies.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Companies.Read)]
    public Task<PagedDataResultDto<CompanyDto>> GetPagedListAsync([FromBody] GetCompaniesPagedDto pagedInput, CancellationToken cancellationToken = default)
        => companies.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Companies.Create)]
    public Task<CompanyDto> CreateAsync([FromBody] CompanyCreateDto input, CancellationToken cancellationToken = default)
        => companies.CreateAsync(input, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Companies.Update)]
    public Task<CompanyDto> UpdateAsync([FromBody] CompanyUpdateDto input, CancellationToken cancellationToken = default)
        => companies.UpdateAsync(input, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Companies.Delete)]
    public Task<CompanyDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => companies.DeleteAsync(id, cancellationToken);
}
