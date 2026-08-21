using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using ChapterAPI.Application.Features.Chapters.Queries;
using ChapterAPI.Application.Features.Chapters.Commands;
using ChapterAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChapterAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChaptersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChaptersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetChapters(
            [FromQuery] Guid? comicId,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                currentUserId = parsedId;
            }

            // Public readers must only receive chapters that have been approved.
            // Admin keeps access to every status for the management screen.
            if (!User.IsInRole("Admin"))
            {
                status = "Published";
            }

            var response = await _mediator.Send(new GetChaptersQuery { ComicId = comicId, Search = search, Status = status, PageNumber = pageNumber, PageSize = pageSize, CurrentUserId = currentUserId });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChapterDetail(Guid id, [FromQuery] bool includePages = false)
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                currentUserId = parsedId;
            }
            // Reader is the normal verified-user role and must still purchase paid chapters.
            bool isAdminOrAuthor = User.IsInRole("Admin");

            var response = await _mediator.Send(new GetChapterDetailQuery { Id = id, IncludePages = includePages, CurrentUserId = currentUserId, IsAdminOrAuthor = isAdminOrAuthor });
            return StatusCode(response.StatusCode, response);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CreateChapter([FromBody] ChapterAPI.DTOs.CreateChapterRequestDto request)
        {
            var response = await _mediator.Send(new CreateChapterCommand { Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] ChapterAPI.DTOs.UpdateChapterRequestDto request)
        {
            var response = await _mediator.Send(new UpdateChapterCommand { Id = id, Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteChapter(Guid id)
        {
            var response = await _mediator.Send(new DeleteChapterCommand { Id = id });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{id}/pages")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> AddPagesBulk(Guid id, [FromForm] System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            var response = await _mediator.Send(new AddPagesBulkCommand { ChapterId = id, Files = files });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("{id}/pages/urls")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> AddPagesByUrls(Guid id, [FromBody] ChapterAPI.DTOs.AddPagesByUrlsRequestDto request)
        {
            var response = await _mediator.Send(new AddPagesByUrlsCommand { ChapterId = id, Urls = request.Urls });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id}/pages")]
        public async Task<IActionResult> GetChapterPages(Guid id)
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                currentUserId = parsedId;
            }
            bool isAdminOrAuthor = User.IsInRole("Admin");

            var response = await _mediator.Send(new GetChapterPagesQuery { Id = id, CurrentUserId = currentUserId, IsAdminOrAuthor = isAdminOrAuthor });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("slug/{slug}/pages")]
        public async Task<IActionResult> GetChapterPagesBySlug([FromQuery] Guid comicId, string slug)
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
            {
                currentUserId = parsedId;
            }
            bool isAdminOrAuthor = User.IsInRole("Admin");

            var response = await _mediator.Send(new GetChapterPagesBySlugQuery { ComicId = comicId, Slug = slug, CurrentUserId = currentUserId, IsAdminOrAuthor = isAdminOrAuthor });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}/pages/reorder")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> ReorderPages(Guid id, [FromBody] System.Collections.Generic.List<ChapterAPI.DTOs.ReorderPageDto> request)
        {
            var response = await _mediator.Send(new ReorderPagesCommand { ChapterId = id, Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id}/pages/{pageId}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> DeletePage(Guid id, Guid pageId)
        {
            var response = await _mediator.Send(new DeletePageCommand { ChapterId = id, PageId = pageId });
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id}/pages")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> DeletePages(
            Guid id,
            [FromBody] DeleteChapterPagesRequestDto request)
        {
            var response = await _mediator.Send(new DeletePagesCommand { ChapterId = id, PageIds = request.PageIds });
            return StatusCode(response.StatusCode, response);
        }
    }
}
