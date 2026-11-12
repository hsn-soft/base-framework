using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/customers")]
public sealed class CustomersController(ICustomerAppService customers) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Customers.Read)]
    public Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customers.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Customers.Read)]
    public Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync([FromBody] GetCustomersPagedDto pagedInput, CancellationToken cancellationToken = default)
        => customers.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Customers.Create)]
    public Task<CustomerDto> CreateAsync([FromBody] CustomerCreateDto input, CancellationToken cancellationToken = default)
        => customers.CreateAsync(input, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Customers.Delete)]
    public Task<CustomerDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => customers.DeleteAsync(id, cancellationToken);
}
