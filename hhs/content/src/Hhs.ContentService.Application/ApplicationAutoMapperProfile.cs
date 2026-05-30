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
            .ForMember(dest => dest.PathFilters,
                opt => opt.MapFrom(source => source.PathFilters
                    .Where(cpf => cpf.PathFilterName != null)
                    .Select(cpf => cpf.PathFilterName)
                    .ToList()));
        CreateMap<Client, ClientCheckDto>()
            .ForMember(dest => dest.PathFilters,
                opt => opt.MapFrom(source => source.PathFilters
                    .Where(cpf => cpf.PathFilterName != null)
                    .Select(x => new KeyValuePair<ClientFilterTypes, string>(x.ClientFilterType, x.PathFilterName))
                    .ToList()));
        CreateMap<Client, ClientSearchDto>();

        CreateMap<AppContent, AppContentDto>()
            .ForMember(dest => dest.ClientDomainName,
                opt => opt.MapFrom(source
                    => source.Client != null ? source.Client.DomainName : string.Empty));
        CreateMap<AppContent, AppContentStatusDto>()
            .ForMember(dest => dest.AppContentId, opt =>
                opt.MapFrom(source => source.Id));
        CreateMap<AppContent, AppContentSearchDto>()
            .ForMember(dest => dest.ClientDomainName,
                opt => opt.MapFrom(source
                    => source.Client != null ? source.Client.DomainName : string.Empty));
    }
}