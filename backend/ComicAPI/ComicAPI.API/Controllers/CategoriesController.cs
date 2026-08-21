using System.Threading.Tasks;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComicAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly MediatR.IMediator _mediator;

        public CategoriesController(MediatR.IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? search = null)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Categories.Queries.GetCategoriesQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize, 
                SearchTerm = search 
            });
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(System.Guid id)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Categories.Queries.GetCategoryByIdQuery { Id = id });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] ComicAPI.Application.DTOs.CreateCategoryRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Categories.Commands.CreateCategoryCommand { Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(System.Guid id, [FromBody] ComicAPI.Application.DTOs.UpdateCategoryRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Categories.Commands.UpdateCategoryCommand { Id = id, Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(System.Guid id)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Categories.Commands.DeleteCategoryCommand { Id = id });
            return StatusCode(response.StatusCode, response);
        }
    }
}
