using AutoMapper;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;

namespace Hhs.AdministrationService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<PermissionGrant, PermissionGrantDto>()
            .ForMember(dest => dest.Name, opt =>
                opt.MapFrom(source => source.Name));
        CreateMap<PermissionGrant, PermissionGrantSearchDto>();

        CreateMap<PermissionGrantDto, PermissionGrant>();

        CreateMap<OldMenuMap, MenuMapDto>();
    }
}