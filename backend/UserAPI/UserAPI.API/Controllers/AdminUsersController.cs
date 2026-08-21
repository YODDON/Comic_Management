using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using UserAPI.Application.DTOs;
using UserAPI.Application.Features.Admin.Commands;
using UserAPI.Application.Features.Admin.Queries;

namespace UserAPI.API.Controllers;

[ApiController]
[Route("auth/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await _mediator.Send(new GetUsersQuery 
        { 
            Search = search, 
            IsActive = isActive, 
            PageNumber = pageNumber, 
            PageSize = pageSize 
        });
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var response = await _mediator.Send(new GetUserQuery { Id = id });
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:int}/lock")]
    public async Task<IActionResult> UpdateLock(int id, [FromBody] UpdateUserLockDto request)
    {
        var response = await _mediator.Send(new UpdateLockCommand 
        { 
            UserId = id, 
            CurrentUserId = GetCurrentUserId() ?? 0, 
            Request = request 
        });
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto request)
    {
        var response = await _mediator.Send(new UpdateRoleCommand 
        { 
            UserId = id, 
            CurrentUserId = GetCurrentUserId() ?? 0, 
            Request = request 
        });
        return StatusCode(response.StatusCode, response);
    }

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
