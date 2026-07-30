using AutoMapper;
using ComicAPI.DTOs;
using ComicAPI.Entities;

namespace ComicAPI.Mappings
{
    public class ComicProfile : Profile
    {
        public ComicProfile()
        {
            CreateMap<Comic, ComicSummaryDto>()
                .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => src.ThumbnailUrl))
                .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
                .ForMember(dest => dest.IsOutstanding, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Comic, ComicDetailDto>()
                .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => src.ThumbnailUrl))
                .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
                .ForMember(dest => dest.IsOutstanding, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.ComicCategories.Select(cc => cc.Category)));
        }
    }
}
