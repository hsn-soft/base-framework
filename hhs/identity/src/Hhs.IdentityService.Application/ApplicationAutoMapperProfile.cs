using AutoMapper;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Domain.AuthDomain.Entities;

namespace Hhs.IdentityService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<AppUser, AppUserDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(source => source.UserName))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(source => source.Email));

        CreateMap<AppRole, AppRoleDto>()
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(source => source.Name));
    }
}