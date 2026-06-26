using AutoMapper;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.SettingDomain.Dtos;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.SettingDomain.Entities;

namespace Hhs.ContentService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<CustomerVpSetting, CustomerVpSettingCheckDto>();

        // CreateMap<CustomerContent, CustomerContentDto>();
        CreateMap<CustomerContent, CustomerContentStatusDto>()
            .ForMember(dest => dest.CustomerContentId, opt =>
                opt.MapFrom(source => source.Id));
        // CreateMap<CustomerContent, CustomerContentSearchDto>();
    }
}