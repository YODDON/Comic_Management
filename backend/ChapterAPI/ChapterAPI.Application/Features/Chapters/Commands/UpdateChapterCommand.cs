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
    public class UpdateChapterCommand : IRequest<ApiResponse<ChapterSummaryDto>>
    {
        public Guid Id { get; set; }
        public UpdateChapterRequestDto Request { get; set; }
    }

    public class UpdateChapterCommandHandler : IRequestHandler<UpdateChapterCommand, ApiResponse<ChapterSummaryDto>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateChapterCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<ChapterSummaryDto>> Handle(UpdateChapterCommand request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.Id, false);
            if (chapter == null)
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<ChapterSummaryDto>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<ChapterSummaryDto>(null, "Rejected comics are read-only. Chapters cannot be edited.", 409);

            var slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Request.Title);
            if (chapter.Slug != slug)
            {
                var isSlugUnique = await _repository.IsSlugUniqueAsync(chapter.ComicId, slug);
                if (!isSlugUnique)
                {
                    return new ApiResponse<ChapterSummaryDto>(null, "Chapter with this title already exists in the comic.", 409);
                }
            }

            chapter.Title = request.Request.Title;
            chapter.Slug = slug;
            chapter.ChapterNumber = request.Request.ChapterNumber;
            chapter.UnitPrice = request.Request.UnitPrice;
            chapter.Status = request.Request.Status;

            await _repository.UpdateChapterAsync(chapter);

            var dto = _mapper.Map<ChapterSummaryDto>(chapter);
            return new ApiResponse<ChapterSummaryDto>(dto, "Chapter updated successfully.");
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
