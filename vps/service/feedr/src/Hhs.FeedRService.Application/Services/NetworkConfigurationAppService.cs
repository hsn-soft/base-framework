using Hhs.FeedRService.Application.Contracts.CustomerDomain;
using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ConfigurationDomain.Models;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;
using HsnSoft.Base;

namespace Hhs.FeedRService.Application.Services;

public sealed class NetworkConfigurationAppService : ApplicationServiceBase, INetworkConfigurationAppService
{
    private readonly INetworkConfigurationRepository _repository;

    public NetworkConfigurationAppService(
        IServiceProvider provider,
        INetworkConfigurationRepository repository) : base(provider)
    {
        _repository = repository;
    }

    public async Task<NetworkConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);
        return MapToDto(entity);
    }

    public async Task<NetworkConfigurationDto> GetByNetworkCodeAsync(string networkCode, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByNetworkCodeAsync(networkCode, cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);
        return MapToDto(entity);
    }

    public async Task<List<NetworkConfigurationDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllActiveAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<NetworkConfigurationDto> CreateAsync(CreateNetworkConfigurationDto input, CancellationToken cancellationToken = default)
    {
        var topLevelGroups = input.TopLevelGroups?.Select(t => new TopLevelGroupConfig(
            t.AdUnitTopLevelCode, t.DisplayName, t.IsActive
        )).ToList() ?? new List<TopLevelGroupConfig>();

        var entity = new NetworkConfiguration(
            networkId: Guid.CreateVersion7(),
            tenantId: input.TenantId,
            networkCode: input.NetworkCode,
            displayName: input.DisplayName,
            isActive: input.IsActive,
            topLevelGroups: topLevelGroups
        );

        entity = await _repository.InsertAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    public async Task<NetworkConfigurationDto> UpdateAsync(Guid id, UpdateNetworkConfigurationDto input, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetSingleOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (entity == null) throw new BusinessException(ApplicationErrorCodes.EntityNotFound);

        entity.SetActive(input.IsActive);

        if (input.TopLevelGroups != null)
        {
            var topLevelGroups = input.TopLevelGroups.Select(t => new TopLevelGroupConfig(
                t.AdUnitTopLevelCode, t.DisplayName, t.IsActive
            )).ToList();

            entity.UpdateTopLevelGroups(topLevelGroups);
        }

        entity = await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(entity);
    }

    private static NetworkConfigurationDto MapToDto(NetworkConfiguration entity) => new()
    {
        Id = entity.Id,
        NetworkId = entity.NetworkId,
        TenantId = entity.TenantId,
        NetworkCode = entity.NetworkCode,
        DisplayName = entity.DisplayName,
        IsActive = entity.IsActive,
        TopLevelGroups = entity.TopLevelGroups?.Select(t => new TopLevelGroupConfigDto
        {
            AdUnitTopLevelCode = t.AdUnitTopLevelCode,
            DisplayName = t.DisplayName,
            IsActive = t.IsActive
        }).ToList() ?? new List<TopLevelGroupConfigDto>()
    };
}
