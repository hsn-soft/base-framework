using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.CompanyDomain.Services;

/// <summary>What can be done with the companies. The controller only calls these; the rules and the data access are behind them.</summary>
public interface ICompanyAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such company.</exception>
    Task<CompanyDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<CompanyDto>> GetPagedListAsync(GetCompaniesPaged pagedInput, CancellationToken cancellationToken = default);

    Task<CompanyDto> CreateAsync(CompanyCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such company.</exception>
    Task UpdateAsync(CompanyUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such company.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
