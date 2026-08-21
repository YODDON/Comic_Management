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

namespace ChapterAPI.Application.Features.Chapters.Queries
{
    public class GetChapterPagesBySlugQuery : IRequest<ApiResponse<List<ChapterPageDto>>>
    {
        public Guid ComicId { get; set; }
        public string Slug { get; set; }
        public int? CurrentUserId { get; set; }
        public bool IsAdminOrAuthor { get; set; }
    }

    public class GetChapterPagesBySlugQueryHandler : IRequestHandler<GetChapterPagesBySlugQuery, ApiResponse<List<ChapterPageDto>>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public GetChapterPagesBySlugQueryHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<List<ChapterPageDto>>> Handle(GetChapterPagesBySlugQuery request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterBySlugAsync(request.ComicId, request.Slug);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (!request.IsAdminOrAuthor && !string.Equals(chapter.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            chapter = await _repository.GetChapterByIdAsync(chapter.Id, true);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (chapter.UnitPrice > 0 && !request.IsAdminOrAuthor)
            {
                if (request.CurrentUserId == null)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần đăng nhập để xem chapter này.", 401);
                }

                var hasPurchased = await _repository.HasUserPurchasedChapterAsync(request.CurrentUserId.Value, chapter.Id);
                if (!hasPurchased)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần mua chapter này.", 403);
                }
            }

            var dtos = _mapper.Map<List<ChapterPageDto>>(chapter.ChapterPages);
            if (!request.IsAdminOrAuthor) await RecordComicReadAsync(chapter.ComicId);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages retrieved successfully.");
        }
    
        private async Task RecordComicReadAsync(Guid comicId)
        {
            try
            {
                await _publishEndpoint.Publish(new ComicViewedIntegrationEvent(comicId));
            }
            catch
            {
            }
        }
    }
}
