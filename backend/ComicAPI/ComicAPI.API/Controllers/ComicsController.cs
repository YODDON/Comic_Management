using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ComicAPI.Application.DTOs;
using SharedKernel.Enums;

namespace ComicAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComicsController : ControllerBase
    {
        private readonly MediatR.IMediator _mediator;

        public ComicsController(MediatR.IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetComics(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] List<Guid>? categoryId = null,
            [FromQuery] ComicStatus? status = null)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetComicsQuery 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize, 
                Search = search, 
                CategoryIds = categoryId, 
                Status = status 
            });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("hot")]
        public async Task<IActionResult> GetHotComics([FromQuery] int limit = 10)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetHotComicsQuery { Limit = limit });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("outstandings")]
        public async Task<IActionResult> GetOutstandingComics([FromQuery] int limit = 10)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetOutstandingComicsListQuery { Limit = limit });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("outstandings/paged")]
        public async Task<IActionResult> GetOutstandingComicsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetOutstandingComicsPagedQuery { PageNumber = pageNumber, PageSize = pageSize });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("last-completed")]
        public async Task<IActionResult> GetLastCompletedComics([FromQuery] int limit = 10)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetLastCompletedComicsQuery { Limit = limit });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetComicBySlug(string slug)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetComicBySlugQuery { Slug = slug });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetComicById(Guid id)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetComicByIdQuery { Id = id });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> CreateComic([FromForm] CreateComicRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.CreateComicCommand { Request = request, UserId = userId });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("upload-cover")]
        [Authorize(Roles = "Admin,Reader")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadCover([FromForm] UploadComicCoverRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.UploadComicCoverCommand { File = request.File });
            return response.Success
                ? Ok(new { data = response.Data })
                : StatusCode(response.StatusCode, new { message = response.Message });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> UpdateComic(Guid id, [FromBody] UpdateComicRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var isAdmin = User.IsInRole("Admin");

            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.UpdateComicCommand { ComicId = id, Request = request, UserId = userId, IsAdmin = isAdmin });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateComicStatus(Guid id, [FromBody] UpdateComicStatusRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.UpdateComicStatusCommand { ComicId = id, Request = request });
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("me")]
        [Authorize(Roles = "Admin,Reader")]
        public async Task<IActionResult> GetMyComics([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetMyComicsQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize });
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("purchased")]
        [Authorize]
        public async Task<IActionResult> GetPurchasedComics([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Queries.GetPurchasedComicsQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize });
            return StatusCode(response.StatusCode, response);
        }
        [HttpPost("outstandings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOutstandingComic([FromBody] CreateOutstandingRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.AddOutstandingComicCommand { Request = request });
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("outstandings/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleOutstandingComic([FromBody] CreateOutstandingRequestDto request)
        {
            var response = await _mediator.Send(new ComicAPI.Application.Features.Comics.Commands.ToggleOutstandingComicCommand { Request = request });
            return StatusCode(response.StatusCode, response);
        }
    }
}
