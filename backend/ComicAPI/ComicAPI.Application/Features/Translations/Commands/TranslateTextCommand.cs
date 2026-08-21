using System.Threading;
using System.Threading.Tasks;
using ComicAPI.Application.DTOs.Translation;
using ComicAPI.Application.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Translations.Commands
{
    public class TranslateTextCommand : IRequest<ApiResponse<TranslateTextResultDto>>
    {
        public TranslateTextRequestDto Request { get; set; } = null!;
    }

    public class TranslateTextCommandHandler : IRequestHandler<TranslateTextCommand, ApiResponse<TranslateTextResultDto>>
    {
        private readonly ITranslationService _translationService;

        public TranslateTextCommandHandler(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        public async Task<ApiResponse<TranslateTextResultDto>> Handle(TranslateTextCommand request, CancellationToken cancellationToken)
        {
            return await _translationService.TranslateAsync(request.Request, cancellationToken);
        }
    }
}
