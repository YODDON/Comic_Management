using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Features.Comics.Helpers;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class UpdateComicCommand : IRequest<ApiResponse<ComicDetailDto>>
    {
        public Guid ComicId { get; set; }
        public UpdateComicRequestDto Request { get; set; } = null!;
        public int UserId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class UpdateComicCommandHandler : IRequestHandler<UpdateComicCommand, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;

        public UpdateComicCommandHandler(IComicRepository comicRepository, IMapper mapper)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(UpdateComicCommand request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.ComicId);
            if (comic == null) return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics are read-only and cannot be edited.", 409);
            }

            if (!request.IsAdmin && comic.OwnerId != request.UserId)
            {
                return new ApiResponse<ComicDetailDto>(null, "You are not authorized to update this comic.", 403);
            }

            comic.Title = request.Request.Title;
            comic.Slug = await ComicHelper.GenerateUniqueSlugAsync(_comicRepository, request.Request.Title, comic.Id);
            comic.Description = request.Request.Description;
            comic.Author = request.Request.Author?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Request.ThumbnailUrl))
            {
                comic.ThumbnailUrl = request.Request.ThumbnailUrl.Trim();
            }
            if (request.IsAdmin)
            {
                if (request.Request.Status.HasValue)
                {
                    comic.Status = request.Request.Status.Value;
                }
            }

            var requestedCategoryIds = (request.Request.CategoryIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            await _comicRepository.UpdateComicAsync(comic, requestedCategoryIds);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic updated successfully.", 200);
        }
    }
}
