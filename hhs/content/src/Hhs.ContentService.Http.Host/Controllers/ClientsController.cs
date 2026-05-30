using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Submits;
using Hhs.ContentService.Application.Contracts.ClientDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/clients")]
public sealed class ClientsController(IServiceProvider provider, IClientAppService clientAppService) : BaseServiceController(provider)
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ClientDto> GetClientAsync(Guid id, CancellationToken cancellationToken = default)
        => await clientAppService.GetAsync(id, cancellationToken);

    // [Authorize(ContentServicePermissions.Clients.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<ClientDto>> GetPagedListAsync([FromBody] GetClientsPaged pagedInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<ClientDto>> GetFilterListAsync([FromBody] GetClientsFilter filterInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<ClientSearchDto>> GetSearchListAsync([FromBody] GetClientsSearch searchInput, CancellationToken cancellationToken = default)
        => await clientAppService.GetSearchListAsync(searchInput, cancellationToken);

    // [Authorize(ContentServicePermissions.Clients.Create)]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ClientDto> CreateAsync([FromBody] ClientCreateDto input)
        => await clientAppService.CreateAsync(input);

    // [Authorize(ContentServicePermissions.Clients.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task UpdateAsync([FromBody] ClientUpdateDto input)
        => await clientAppService.UpdateAsync(input);

    // [Authorize(ContentServicePermissions.Clients.Delete)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task DeleteAsync(Guid id)
        => await clientAppService.DeleteAsync(id);
}