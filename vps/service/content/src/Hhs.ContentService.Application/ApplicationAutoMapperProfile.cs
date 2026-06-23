using AutoMapper;

namespace Hhs.ContentService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        // CreateMap<CustomerVpSetting, CustomerVpSettingCheckDto>();
        //
        // CreateMap<CustomerContent, CustomerContentDto>();
        // CreateMap<CustomerContent, CustomerContentStatusDto>()
        //     .ForMember(dest => dest.CustomerContentId, opt =>
        //         opt.MapFrom(source => source.Id));
        // CreateMap<CustomerContent, CustomerContentSearchDto>();
    }
}