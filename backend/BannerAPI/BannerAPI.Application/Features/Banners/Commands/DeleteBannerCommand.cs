using System;
using System.Threading;
using System.Threading.Tasks;
using BannerAPI.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace BannerAPI.Application.Features.Banners.Commands
{
    public record DeleteBannerCommand(Guid Id) : IRequest<ApiResponse<bool>>;

    public class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand, ApiResponse<bool>>
    {
        private readonly IBannerRepository _repository;
        private readonly ICloudinaryService _cloudinaryService;

        public DeleteBannerCommandHandler(IBannerRepository repository, ICloudinaryService cloudinaryService)
        {
            _repository = repository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteBannerCommand command, CancellationToken cancellationToken)
        {
            var banner = await _repository.GetByIdAsync(command.Id);
            if (banner is null)
            {
                return ApiResponse<bool>.ErrorResponse("Banner not found.", 404);
            }

            if (!string.IsNullOrWhiteSpace(banner.ImagePublicId)
                && !await _cloudinaryService.DeleteImageAsync(banner.ImagePublicId))
            {
                return ApiResponse<bool>.ErrorResponse("Could not delete the banner image from Cloudinary.", 502);
            }

            _repository.Remove(banner);
            await _repository.SaveChangesAsync();
            
            return new ApiResponse<bool>(true, "Banner deleted successfully.");
        }
    }
}
