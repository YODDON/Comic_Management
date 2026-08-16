using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;

namespace ComicAPI.Application.Mappings
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDto>();
        }
    }
}
