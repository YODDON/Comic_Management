using BannerAPI.DTOs;
using BannerAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BannerAPI.Controllers;

[ApiController]
[Route("banners")]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var response = await _bannerService.GetActiveAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var response = await _bannerService.GetAllAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await _bannerService.GetByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBannerRequestDto request)
    {
        var response = await _bannerService.CreateAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadBannerImageRequestDto request,
        [FromServices] ICloudinaryService cloudinary)
    {
        try
        {
            var result = await cloudinary.UploadImageAsync(request.File);
            return Ok(new { data = new { imageUrl = result.Url, imagePublicId = result.PublicId } });
        }
        catch (ArgumentException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBannerRequestDto request)
    {
        var response = await _bannerService.UpdateAsync(id, request);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _bannerService.DeleteAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}
