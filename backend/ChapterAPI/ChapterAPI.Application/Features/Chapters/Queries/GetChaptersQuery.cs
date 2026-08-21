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
    public class GetChaptersQuery : IRequest<ApiResponse<PagedResult<ChapterSummaryDto>>>
    {
        public Guid? ComicId { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int? CurrentUserId { get; set; }
    }

    public class GetChaptersQueryHandler : IRequestHandler<GetChaptersQuery, ApiResponse<PagedResult<ChapterSummaryDto>>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public GetChaptersQueryHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<PagedResult<ChapterSummaryDto>>> Handle(GetChaptersQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _repository.GetChaptersAsync(request.ComicId, request.Search, request.Status, request.PageNumber, request.PageSize);

            var dtos = _mapper.Map<List<ChapterSummaryDto>>(items);
            if (request.CurrentUserId.HasValue)
            {
                var paidChapterIds = items
                    .Where(chapter => chapter.UnitPrice > 0)
                    .Select(chapter => chapter.Id)
                    .ToList();
                var purchasedChapterIds = await _repository.GetPurchasedChapterIdsAsync(
                    request.CurrentUserId.Value, paidChapterIds);
                foreach (var dto in dtos)
                {
                    dto.IsPurchased = purchasedChapterIds.Contains(dto.Id);
                }
            }

            var pagedResult = new PagedResult<ChapterSummaryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = request.PageNumber,
                PageSize = request.PageSize
            };

            return new ApiResponse<PagedResult<ChapterSummaryDto>>(pagedResult, "Chapters retrieved successfully.");
        }
    }
}
