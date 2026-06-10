using AutoMapper;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.CustomerDomain.Entities;
using Hhs.ContentService.Domain.Enums;

namespace Hhs.ContentService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<CustomerContentSetting, CustomerContentSettingDto>()
            .ForMember(dest => dest.PathFilters,
                opt => opt.MapFrom(source => source.PathFilters
                    .Where(cpf => cpf.PathFilterName != null)
                    .Select(cpf => cpf.PathFilterName)
                    .ToList()));
        CreateMap<CustomerContentSetting, CustomerContentSettingCheckDto>()
            .ForMember(dest => dest.PathFilters,
                opt => opt.MapFrom(source => source.PathFilters
                    .Where(cpf => cpf.PathFilterName != null)
                    .Select(x => new KeyValuePair<CustomerSettingFilterTypes, string>(x.CustomerSettingFilterType, x.PathFilterName))
                    .ToList()));
        CreateMap<CustomerContentSetting, CustomerContentSettingSearchDto>();

        CreateMap<AppContent, AppContentDto>();
        CreateMap<AppContent, AppContentStatusDto>()
            .ForMember(dest => dest.AppContentId, opt =>
                opt.MapFrom(source => source.Id));
        CreateMap<AppContent, AppContentSearchDto>();
    }
}