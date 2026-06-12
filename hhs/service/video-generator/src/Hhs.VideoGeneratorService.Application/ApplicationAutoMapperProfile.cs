using AutoMapper;
using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using HsnSoft.Base.Text;

namespace Hhs.VideoGeneratorService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<EncodedNormalizedContentData, NormalizedContentData>()
            .ForMember(dest => dest.TitleText, opt =>
                opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedTitleText)))
            .ForMember(dest => dest.NormalizedContent, opt =>
                opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedNormalizedContent)))
            .ForMember(dest => dest.ImageUrl, opt =>
                opt.MapFrom(source => StringHelper.Base64Decode(source.EncodedImageUrl))
            );

        CreateMap<NormalizedContentData, VideoContentDataDto>();
    }
}