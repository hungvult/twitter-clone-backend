using System.Text.Json;
using AutoMapper;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Following, opt => opt.MapFrom(src => DeserializeStringList(src.Following)))
            .ForMember(dest => dest.Followers, opt => opt.MapFrom(src => DeserializeStringList(src.Followers)));
        
        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Following, opt => opt.MapFrom(src => SerializeObject(src.Following)))
            .ForMember(dest => dest.Followers, opt => opt.MapFrom(src => SerializeObject(src.Followers)));

        // Tweet mappings
        CreateMap<Tweet, TweetDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => DeserializeImages(src.Images)))
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => CreateParentInfo(src.ParentId, src.ParentUsername)))
            .ForMember(dest => dest.UserLikes, opt => opt.MapFrom(src => DeserializeStringList(src.UserLikes)))
            .ForMember(dest => dest.UserRetweets, opt => opt.MapFrom(src => DeserializeStringList(src.UserRetweets)));
        
        CreateMap<TweetDto, Tweet>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => SerializeObject(src.Images)))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Id : null))
            .ForMember(dest => dest.ParentUsername, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Username : null))
            .ForMember(dest => dest.UserLikes, opt => opt.MapFrom(src => SerializeObject(src.UserLikes)))
            .ForMember(dest => dest.UserRetweets, opt => opt.MapFrom(src => SerializeObject(src.UserRetweets)));

        // Bookmark mappings
        CreateMap<Bookmark, BookmarkDto>();
        CreateMap<BookmarkDto, Bookmark>();
    }

    private static List<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>();
    }

    private static List<ImageData>? DeserializeImages(string? json)
    {
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<List<ImageData>>(json, (JsonSerializerOptions?)null);
    }

    private static ParentTweetInfo? CreateParentInfo(string? parentId, string? parentUsername)
    {
        return string.IsNullOrEmpty(parentId) ? null : new ParentTweetInfo { Id = parentId, Username = parentUsername ?? "" };
    }

    private static string? SerializeObject(object? obj)
    {
        return obj == null ? null : JsonSerializer.Serialize(obj, (JsonSerializerOptions?)null);
    }
}
