using AutoMapper;
using HsnSoft.Base.Text;

namespace Hhs.TextNormalizerService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        // CreateMap<ContentNormalizedRequest, ContentNormalizedRequestDto>();
        // CreateMap<ContentNormalizedRequest, ContentNormalizedRequestSearchDto>();
        //
        // CreateMap<AnalysisNormalizedRequest, AnalysisNormalizedRequestDto>();
        //
        // CreateMap<ScrapingContentDataModel, AnalysisDataModel>();
        //
        // CreateMap<AnalysisReferenceModel, EncodedNormalizedContentData>()
        //     .ForMember(dest => dest.EncodedTitleText, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Encode(source.AnalysisDataModel.Title)))
        //     .ForMember(dest => dest.EncodedNormalizedContent, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Encode(source.OutlineContentData)))
        //     .ForMember(dest => dest.EncodedImageUrl, opt =>
        //         opt.MapFrom(source => StringHelper.Base64Encode(source.AnalysisDataModel.ImageUrl))
        //     );
    }
}