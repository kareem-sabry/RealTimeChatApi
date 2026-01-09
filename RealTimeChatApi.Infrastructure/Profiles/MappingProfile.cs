using AutoMapper;
using RealTimeChatApi.Application.Dtos.Message;
using RealTimeChatApi.Application.Dtos.User;
using RealTimeChatApi.Core.Entities;

namespace RealTimeChatApi.Infrastructure.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.FullName));
    }
}