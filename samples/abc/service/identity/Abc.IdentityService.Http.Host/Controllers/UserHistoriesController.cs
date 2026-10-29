using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos.Submits;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Services;
using Abc.IdentityService.Http.Host.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Abc.IdentityService.Http.Host.Controllers;

/// <summary>The history of the users (kept in its own database): every action only calls the application service, which holds the rules.</summary>
[Route("api/identity-service/v1/commercial/user-histories")]
public sealed class UserHistoriesController(IUserHistoryAppService historyAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<UserHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => historyAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<UserHistoryDto>> GetPagedListAsync([FromBody] GetUserHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
        => historyAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<UserHistoryDto>> GetFilterListAsync([FromBody] GetUserHistoriesFilter filterInput, CancellationToken cancellationToken = default)
        => historyAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<UserHistorySearchDto>> GetSearchListAsync([FromBody] GetUserHistoriesSearch searchInput, CancellationToken cancellationToken = default)
        => historyAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] UserHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        UserHistoryDto created = await historyAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] UserHistoryUpdateDto input, CancellationToken cancellationToken = default)
    {
        await historyAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await historyAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
