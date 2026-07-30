using ComicAPI.DTOs.Translation;
using ComicAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComicAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TranslationsController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public TranslationsController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpPost]
    public async Task<IActionResult> Translate(
        [FromBody] TranslateTextRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _translationService.TranslateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
