using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Abc.IdentityService.Application.Contracts.UserHistoryDomain.Services;

/// <summary>What can be done with the history of the users. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IUserHistoryAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task<UserHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the records that match the filters, the latest first unless a sorting is given, with how many match in all.</summary>
    Task<PagedDataResultDto<UserHistoryDto>> GetPagedListAsync(GetUserHistoriesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the records that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<UserHistoryDto>> GetFilterListAsync(GetUserHistoriesFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The records that have the text in their user name or action, shown briefly.</summary>
    Task<List<UserHistorySearchDto>> GetSearchListAsync(GetUserHistoriesSearch searchInput, CancellationToken cancellationToken = default);

    Task<UserHistoryDto> CreateAsync(UserHistoryCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task UpdateAsync(UserHistoryUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
