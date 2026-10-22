using AutoMapper;
using HsnSoft.Base.Sample.Api.Application.Contracts;
using HsnSoft.Base.Sample.Api.Domain.Entities;

namespace HsnSoft.Base.Sample.Api.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
    }
}