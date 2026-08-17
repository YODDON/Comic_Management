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
    public class RemoveFavoriteCommand : IRequest<ApiResponse<bool>>
    {
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
    }

    public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, ApiResponse<bool>>
    {
        private readonly IFavoriteRepository _repository;
        private readonly IComicValidator _comicValidator;

        public RemoveFavoriteCommandHandler(IFavoriteRepository repository, IComicValidator comicValidator)
        {
            _repository = repository;
            _comicValidator = comicValidator;
        }

        public async Task<ApiResponse<bool>> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
        {
            var existingFav = await _repository.GetFavoriteAsync(request.UserId, request.ComicId);
            if (existingFav == null)
            {
                return new ApiResponse<bool>(false, "Comic is not in favorites.", 404);
            }

            await _repository.RemoveFavoriteAsync(existingFav);

            return new ApiResponse<bool>(true, "Removed from favorites successfully.");
        }
    }
}
