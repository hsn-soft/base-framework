using Hhs.FeedRService.Application.Contracts.CustomerDomain;
using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;
using HsnSoft.Base;

namespace Hhs.FeedRService.Application.Services;

public sealed class CustomerConfigurationAppService : ApplicationServiceBase, ICustomerConfigurationAppService
{
    private readonly ICustomerConfigurationRepository _repository;

    public CustomerConfigurationAppService(
        IServiceProvider provider,
        ICustomerConfigurationRepository repository) : base(provider)
    {
        _repository = repository;
    }

    public async Task<CustomerConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);
        return MapToDto(entity);
    }

    public async Task<CustomerConfigurationDto> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.FindByUniqueKeysAsync(clientId, cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);
        return MapToDto(entity);
    }

    public async Task<List<CustomerConfigurationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<CustomerConfigurationDto> CreateAsync(CreateCustomerConfigurationDto input, CancellationToken cancellationToken = default)
    {
        var entity = new CustomerConfiguration(
            id: Guid.NewGuid(),
            tenantId: input.TenantId,
            clientId: input.ClientId,
            clientName: input.ClientName,
            network: input.Network,
            adUnitName: input.AdUnitName,
            adUnitIdTopLevel: input.AdUnitIdTopLevel,
            adUnitId: input.AdUnitId ?? new List<string>()
        );

        entity = await _repository.InsertAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    public async Task<CustomerConfigurationDto> UpdateAsync(Guid id, UpdateCustomerConfigurationDto input, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);

        entity.Update(
            clientName: input.ClientName,
            network: input.Network,
            adUnitName: input.AdUnitName,
            adUnitIdTopLevel: input.AdUnitIdTopLevel,
            adUnitId: input.AdUnitId ?? new List<string>()
        );

        entity = await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    private static CustomerConfigurationDto MapToDto(CustomerConfiguration entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        ClientId = entity.ClientId,
        ClientName = entity.ClientName,
        Network = entity.Network,
        AdUnitName = entity.AdUnitName,
        AdUnitIdTopLevel = entity.AdUnitIdTopLevel,
        AdUnitId = entity.AdUnitId
    };
}
