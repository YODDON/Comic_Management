using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Application.Features.Favorites.Commands
{
    public class AddFavoriteCommand : IRequest<ApiResponse<FavoriteDto>>
    {
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
    }

    public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, ApiResponse<FavoriteDto>>
    {
        private readonly IFavoriteRepository _repository;
        private readonly IComicValidator _comicValidator;

        public AddFavoriteCommandHandler(IFavoriteRepository repository, IComicValidator comicValidator)
        {
            _repository = repository;
            _comicValidator = comicValidator;
        }

        public async Task<ApiResponse<FavoriteDto>> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
        {
            var exists = await _comicValidator.ExistsAsync(request.ComicId);
            if (!exists)
            {
                return new ApiResponse<FavoriteDto>(null, "Comic not found.", 404);
            }

            var existingFav = await _repository.GetFavoriteAsync(request.UserId, request.ComicId);
            if (existingFav != null)
            {
                return new ApiResponse<FavoriteDto>(null, "Comic is already in favorites.", 409);
            }

            var fav = new Favorite
            {
                UserId = request.UserId,
                ComicId = request.ComicId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddFavoriteAsync(fav);

            var dto = new FavoriteDto
            {
                Id = fav.Id,
                ComicId = fav.ComicId,
                CreatedAt = fav.CreatedAt
            };

            return new ApiResponse<FavoriteDto>(dto, "Added to favorites successfully.");
        }
    }
}
