using AutoMapper;
using HsnSoft.Base.Test.Api.Application.Contracts;
using HsnSoft.Base.Test.Api.Domain.Entities;

namespace HsnSoft.Base.Test.Api.Application;

public class ApplicationAutoMapperProfile : Profile
{
    public ApplicationAutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
    }
}