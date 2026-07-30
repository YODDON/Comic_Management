using ComicAPI.DTOs.Translation;
using SharedKernel.Responses;

namespace ComicAPI.Interfaces;

public interface ITranslationService
{
    Task<ApiResponse<TranslateTextResultDto>> TranslateAsync(
        TranslateTextRequestDto request,
        CancellationToken cancellationToken);
}
