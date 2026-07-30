using AutoMapper;
using ComicAPI.DTOs;
using ComicAPI.Entities;

namespace ComicAPI.Mappings
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDto>();
        }
    }
}
