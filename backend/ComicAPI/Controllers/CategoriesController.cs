using System.Threading.Tasks;
using ComicAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? search = null)
        {
            var response = await _categoryService.GetCategoriesAsync(pageNumber, pageSize, search);
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(System.Guid id)
        {
            var response = await _categoryService.GetCategoryByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] ComicAPI.DTOs.CreateCategoryRequestDto request)
        {
            var response = await _categoryService.CreateCategoryAsync(request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(System.Guid id, [FromBody] ComicAPI.DTOs.UpdateCategoryRequestDto request)
        {
            var response = await _categoryService.UpdateCategoryAsync(id, request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(System.Guid id)
        {
            var response = await _categoryService.DeleteCategoryAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}
