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
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant.Name))
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(source => source.UserRoles.Select(ur => ur.Role.Name)));

        CreateMap<AppRole, AppRoleDto>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant.Name));
    }
}