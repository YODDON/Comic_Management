using ComicAPI.Application.DTOs.Translation;
using SharedKernel.Responses;

namespace ComicAPI.Application.Interfaces;

public interface ITranslationService
{
    Task<ApiResponse<TranslateTextResultDto>> TranslateAsync(
        TranslateTextRequestDto request,
        CancellationToken cancellationToken);
}
