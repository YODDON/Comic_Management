using System.Threading;
using System.Threading.Tasks;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class UploadComicCoverCommand : IRequest<ApiResponse<ComicCoverUploadDto>>
    {
        public IFormFile File { get; set; } = null!;
    }

    public class UploadComicCoverCommandHandler : IRequestHandler<UploadComicCoverCommand, ApiResponse<ComicCoverUploadDto>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadComicCoverCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<ComicCoverUploadDto>> Handle(UploadComicCoverCommand request, CancellationToken cancellationToken)
        {
            var imageUrl = await _cloudinaryService.UploadImageAsync(request.File);
            return string.IsNullOrWhiteSpace(imageUrl)
                ? ApiResponse<ComicCoverUploadDto>.ErrorResponse("Không thể tải ảnh bìa lên.", 400)
                : new ApiResponse<ComicCoverUploadDto>(
                    new ComicCoverUploadDto { ThumbnailUrl = imageUrl },
                    "Comic cover uploaded successfully.");
        }
    }
}
