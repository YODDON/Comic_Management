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
    public class CreateChapterCommand : IRequest<ApiResponse<ChapterSummaryDto>>
    {
        public CreateChapterRequestDto Request { get; set; }
    }

    public class CreateChapterCommandHandler : IRequestHandler<CreateChapterCommand, ApiResponse<ChapterSummaryDto>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateChapterCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<ChapterSummaryDto>> Handle(CreateChapterCommand request, CancellationToken cancellationToken)
        {
            bool exists;
            try
            {
                exists = await _comicValidator.ExistsAsync(request.Request.ComicId);
            }
            catch (Exception ex)
            {
                return ApiResponse<ChapterSummaryDto>.ErrorResponse(
                    $"Comic validation failed: {ex.Message}",
                    503);
            }

            if (!exists)
            {
                return ApiResponse<ChapterSummaryDto>.ErrorResponse("The specified Comic does not exist or its status prevents adding chapters.", 404);
            }

            var slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Request.Title);
            var isSlugUnique = await _repository.IsSlugUniqueAsync(request.Request.ComicId, slug);
            if (!isSlugUnique)
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Chapter with this title already exists in the comic.", 409);
            }

            var chapter = new ChapterAPI.Entities.Chapter
            {
                ComicId = request.Request.ComicId,
                Title = request.Request.Title,
                Slug = slug,
                ChapterNumber = request.Request.ChapterNumber,
                UnitPrice = request.Request.UnitPrice,
                Status = request.Request.Status,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddChapterAsync(chapter);

            var dto = _mapper.Map<ChapterSummaryDto>(chapter);
            return new ApiResponse<ChapterSummaryDto>(dto, "Chapter created successfully.");
        }
    }
}
