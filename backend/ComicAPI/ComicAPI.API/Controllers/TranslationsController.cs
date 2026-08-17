using ComicAPI.Application.DTOs.Translation;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComicAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TranslationsController : ControllerBase
{
    private readonly MediatR.IMediator _mediator;

    public TranslationsController(MediatR.IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Translate(
        [FromBody] TranslateTextRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ComicAPI.Application.Features.Translations.Commands.TranslateTextCommand { Request = request }, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
