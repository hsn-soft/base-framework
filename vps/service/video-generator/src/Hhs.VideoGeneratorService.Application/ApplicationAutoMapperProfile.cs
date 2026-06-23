using AutoMapper;

namespace Hhs.VideoGeneratorService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        // CreateMap<EncodedNormalizedContentData, NormalizedContentData>()
        //     .ForMember(dest => dest.TitleText, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedTitleText)))
        //     .ForMember(dest => dest.NormalizedContent, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedNormalizedContent)))
        //     .ForMember(dest => dest.ImageUrl, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedImageUrl))
        //     );
        //
        // CreateMap<NormalizedContentData, VideoContentDataDto>();
    }
}