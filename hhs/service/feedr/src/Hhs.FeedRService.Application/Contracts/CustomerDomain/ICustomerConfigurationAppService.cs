using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

namespace Hhs.FeedRService.Application.Contracts.CustomerDomain;

public interface ICustomerConfigurationAppService
{
    Task<CustomerConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerConfigurationDto> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<List<CustomerConfigurationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerConfigurationDto> CreateAsync(CreateCustomerConfigurationDto input, CancellationToken cancellationToken = default);
    Task<CustomerConfigurationDto> UpdateAsync(Guid id, UpdateCustomerConfigurationDto input, CancellationToken cancellationToken = default);
}
