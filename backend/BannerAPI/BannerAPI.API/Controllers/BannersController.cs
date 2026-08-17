using BannerAPI.Application.Features.Banners.Commands;
using BannerAPI.Application.Features.Banners.Queries;
using BannerAPI.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BannerAPI.Controllers;

[ApiController]
[Route("banners")]
public class BannersController : ControllerBase
{
    private readonly IMediator _mediator;

    public BannersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var response = await _mediator.Send(new GetActiveBannersQuery());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var response = await _mediator.Send(new GetAllBannersQuery());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _mediator.Send(new GetBannerByIdQuery(id));
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBannerRequestDto request)
    {
        var response = await _mediator.Send(new CreateBannerCommand(request));
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadBannerImageRequestDto request)
    {
        var response = await _mediator.Send(new UploadBannerImageCommand(request.File));
        return response.Success
            ? Ok(new { data = response.Data })
            : StatusCode(response.StatusCode, new { message = response.Message });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBannerRequestDto request)
    {
        var response = await _mediator.Send(new UpdateBannerCommand(id, request));
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _mediator.Send(new DeleteBannerCommand(id));
        return StatusCode(response.StatusCode, response);
    }
}
