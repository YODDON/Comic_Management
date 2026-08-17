using System;
using System.Threading;
using System.Threading.Tasks;
using BannerAPI.DTOs;
using BannerAPI.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.Responses;

namespace BannerAPI.Application.Features.Banners.Commands
{
    public record UploadBannerImageCommand(IFormFile File) : IRequest<ApiResponse<BannerImageUploadDto>>;

    public class UploadBannerImageCommandHandler : IRequestHandler<UploadBannerImageCommand, ApiResponse<BannerImageUploadDto>>
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadBannerImageCommandHandler(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<BannerImageUploadDto>> Handle(UploadBannerImageCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _cloudinaryService.UploadImageAsync(command.File);
                var dto = new BannerImageUploadDto
                {
                    ImageUrl = result.Url,
                    ImagePublicId = result.PublicId
                };
                return new ApiResponse<BannerImageUploadDto>(dto, "Banner image uploaded successfully.");
            }
            catch (ArgumentException error)
            {
                return ApiResponse<BannerImageUploadDto>.ErrorResponse(error.Message, 400);
            }
        }
    }
}
