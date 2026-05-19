using AutoMapper;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;

namespace Hhs.ContentService.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<Client, ClientDto>()
            .ForMember(dest => dest.PathFilters, opt =>
                opt.MapFrom(source => source.PathFilters.Select(x => x.PathFilterName).ToList()));
        CreateMap<Client, ClientCheckDto>()
            .ForMember(dest => dest.PathFilters, opt =>
                opt.MapFrom(source => source.PathFilters.Select(x => new KeyValuePair<ClientFilterTypes, string>(x.ClientFilterType, x.PathFilterName)).ToList()));
        CreateMap<Client, ClientSearchDto>();

        CreateMap<AppContent, AppContentDto>();
        CreateMap<AppContent, AppContentStatusDto>()
            .ForMember(dest => dest.AppContentId, opt =>
                opt.MapFrom(source => source.Id));
    }
}