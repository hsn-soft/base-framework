using AutoMapper;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos;
using Hhs.EventManagerService.Domain.EventDomain.Entities;

namespace Hhs.EventManagerService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<FailedIntegrationEvent, FailedIntegrationEventDto>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(source => source.Id)).ReverseMap();
    }
}