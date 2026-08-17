using AutoMapper;
using ChapterAPI.DTOs;
using ChapterAPI.Entities;

namespace ChapterAPI.Mappings
{
    public class ChapterMappingProfile : Profile
    {
        public ChapterMappingProfile()
        {
            CreateMap<Chapter, ChapterSummaryDto>();
            CreateMap<Chapter, ChapterDetailDto>();
            CreateMap<ChapterPage, ChapterPageDto>();
        }
    }
}
