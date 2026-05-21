using AutoMapper;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.Shared.Helper.Utils;

namespace Hhs.IdentityService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<AppUser, AppUserDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(source => StringOperations.SplitFirstValue(source.UserName, "#")))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(source => StringOperations.SplitFirstValue(source.Email, "#")));

        CreateMap<AppRole, AppRoleDto>()
            .ForMember(dest => dest.Name,
                opt => opt.MapFrom(source => StringOperations.SplitFirstValue(source.Name, "#")));
    }
}