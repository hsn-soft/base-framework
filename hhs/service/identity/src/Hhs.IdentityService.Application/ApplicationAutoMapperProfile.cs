using AutoMapper;
using Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;

namespace Hhs.IdentityService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<AppUser, AppUserDto>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant != null ? source.Tenant.Name : string.Empty))
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(source => source.UserRoles
                        .Where(ur => ur.Role != null)
                        .Select(ur => ur.Role.Name)
                        .ToList()));
        CreateMap<AppUser, AppUserSearchDto>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant != null ? source.Tenant.Name : string.Empty));

        CreateMap<AppRole, AppRoleDto>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant != null ? source.Tenant.Name : string.Empty));

        CreateMap<AppRole, AppRoleSearchDto>()
            .ForMember(dest => dest.TenantName,
                opt => opt.MapFrom(source => source.Tenant != null ? source.Tenant.Name : string.Empty));


        CreateMap<AppContent, AppContentListDto>()
            .ForMember(
                dest => dest.ClientDomain,
                opt => opt.MapFrom(src => src.Client.Domain))
            .ForMember(
                dest => dest.ProductTypeCode,
                opt => opt.MapFrom(src => src.ProductType.Code));

    }
}