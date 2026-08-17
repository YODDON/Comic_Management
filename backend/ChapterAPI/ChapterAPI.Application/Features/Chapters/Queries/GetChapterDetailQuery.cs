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
    public class GetChapterDetailQuery : IRequest<ApiResponse<ChapterDetailDto>>
    {
        public Guid Id { get; set; }
        public bool IncludePages { get; set; }
        public int? CurrentUserId { get; set; }
        public bool IsAdminOrAuthor { get; set; }
    }

    public class GetChapterDetailQueryHandler : IRequestHandler<GetChapterDetailQuery, ApiResponse<ChapterDetailDto>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public GetChapterDetailQueryHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<ChapterDetailDto>> Handle(GetChapterDetailQuery request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.Id, request.IncludePages);
            if (chapter == null)
            {
                return new ApiResponse<ChapterDetailDto>(null, "Chapter not found.", 404);
            }

            if (!request.IsAdminOrAuthor && !string.Equals(chapter.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<ChapterDetailDto>(null, "Chapter not found.", 404);
            }

            var dto = _mapper.Map<ChapterDetailDto>(chapter);

            if (request.IncludePages)
            {
                if (chapter.UnitPrice > 0 && !request.IsAdminOrAuthor)
                {
                    if (request.CurrentUserId == null)
                    {
                        return new ApiResponse<ChapterDetailDto>(null, "Authentication required to view VIP chapter.", 401);
                    }

                    var hasPurchased = await _repository.HasUserPurchasedChapterAsync(request.CurrentUserId.Value, request.Id);
                    if (!hasPurchased)
                    {
                        dto.ChapterPages = new List<ChapterPageDto>();
                        return new ApiResponse<ChapterDetailDto>(dto, "Bạn cần mua chapter này.", 403);
                    }
                }
            }

            return new ApiResponse<ChapterDetailDto>(dto, "Chapter details retrieved successfully.");
        }
    }
}
