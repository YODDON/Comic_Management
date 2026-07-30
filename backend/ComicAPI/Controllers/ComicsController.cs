using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComicAPI.Entities;
using ComicAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ComicAPI.DTOs;
using SharedKernel.Enums;

namespace ComicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComicsController : ControllerBase
    {
        private readonly IComicService _comicService;

        public ComicsController(IComicService comicService)
        {
            _comicService = comicService;
        }

        [HttpGet]
        public async Task<IActionResult> GetComics(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] List<Guid>? categoryId = null,
            [FromQuery] ComicStatus? status = null)
        {
            var response = await _comicService.GetComicsAsync(pageNumber, pageSize, search, categoryId, status);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("hot")]
        public async Task<IActionResult> GetHotComics([FromQuery] int limit = 10)
        {
            var response = await _comicService.GetHotComicsAsync(limit);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("outstandings")]
        public async Task<IActionResult> GetOutstandingComics([FromQuery] int limit = 10)
        {
            var response = await _comicService.GetOutstandingComicsAsync(limit);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("outstandings/paged")]
        public async Task<IActionResult> GetOutstandingComicsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var response = await _comicService.GetOutstandingComicsAsync(pageNumber, pageSize);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("last-completed")]
        public async Task<IActionResult> GetLastCompletedComics([FromQuery] int limit = 10)
        {
            var response = await _comicService.GetLastCompletedComicsAsync(limit);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetComicBySlug(string slug)
        {
            var response = await _comicService.GetComicBySlugAsync(slug);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetComicById(Guid id)
        {
            var response = await _comicService.GetComicByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CreateComic([FromForm] CreateComicRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _comicService.CreateComicAsync(request, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("upload-cover")]
        [Authorize(Roles = "Admin,Reader")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadCover(
            [FromForm] UploadComicCoverRequestDto request,
            [FromServices] ICloudinaryService cloudinaryService)
        {
            var imageUrl = await cloudinaryService.UploadImageAsync(request.File);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(new { message = "Không thể tải ảnh bìa lên." });
            }

            return Ok(new { data = new { thumbnailUrl = imageUrl } });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> UpdateComic(Guid id, [FromBody] UpdateComicRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var isAdmin = User.IsInRole("Admin");

            var response = await _comicService.UpdateComicAsync(id, request, userId, isAdmin);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateComicStatus(Guid id, [FromBody] UpdateComicStatusRequestDto request)
        {
            var response = await _comicService.UpdateComicStatusAsync(id, request);
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("me")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetMyComics([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _comicService.GetMyComicsAsync(userId, pageNumber, pageSize);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("purchased")]
        [Authorize]
        public async Task<IActionResult> GetPurchasedComics([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _comicService.GetPurchasedComicsAsync(userId, pageNumber, pageSize);
            return StatusCode(response.StatusCode, response);
        }
        [HttpPost("outstandings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOutstandingComic([FromBody] CreateOutstandingRequestDto request)
        {
            var response = await _comicService.AddOutstandingComicAsync(request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("outstandings/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleOutstandingComic([FromBody] CreateOutstandingRequestDto request)
        {
            var response = await _comicService.ToggleOutstandingComicAsync(request);
            return StatusCode(response.StatusCode, response);
        }
    }
}
