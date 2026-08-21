using AutoMapper;
using MassTransit;
using SharedKernel.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using ChapterAPI.DTOs;
using ChapterAPI.Entities;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Features.Chapters.Commands
{
    public class ReorderPagesCommand : IRequest<ApiResponse<bool>>
    {
        public Guid ChapterId { get; set; }
        public List<ReorderPageDto> Request { get; set; }
    }

    public class ReorderPagesCommandHandler : IRequestHandler<ReorderPagesCommand, ApiResponse<bool>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public ReorderPagesCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(ReorderPagesCommand request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.ChapterId, true);
            if (chapter == null)
            {
                return new ApiResponse<bool>(false, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<bool>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<bool>(false, "Rejected comics are read-only. Chapter pages cannot be reordered.", 409);

            var pagesToUpdate = new List<ChapterAPI.Entities.ChapterPage>();
            var newOrderNumbers = new HashSet<int>();

            foreach (var item in request.Request)
            {
                var page = chapter.ChapterPages.FirstOrDefault(p => p.Id == item.PageId);
                if (page == null)
                {
                    return new ApiResponse<bool>(false, $"PageId {item.PageId} does not belong to this chapter.", 400);
                }

                if (!newOrderNumbers.Add(item.NewOrder))
                {
                    return new ApiResponse<bool>(false, $"Duplicate pageNumber {item.NewOrder} in request.Request.", 400);
                }

                page.PageNumber = item.NewOrder;
                pagesToUpdate.Add(page);
            }

            // Also ensure that across ALL pages in chapter, there are no duplicates.
            // Since we might only update a subset, we must verify the entire set.
            var allFinalNumbers = new HashSet<int>();
            foreach (var p in chapter.ChapterPages)
            {
                if (!allFinalNumbers.Add(p.PageNumber))
                {
                    return new ApiResponse<bool>(false, $"Reordering results in duplicate pageNumber {p.PageNumber}.", 400);
                }
            }

            if (pagesToUpdate.Any())
            {
                await _repository.UpdateChapterPagesAsync(pagesToUpdate);
            }

            return new ApiResponse<bool>(true, "Pages reordered successfully.");
        }
    
        private async Task<bool?> IsRejectedComicAsync(Guid comicId)
        {
            try
            {
                bool exists = await _comicValidator.ExistsAsync(comicId);
                return !exists;
            }
            catch
            {
                return null;
            }
        }
    }
}
