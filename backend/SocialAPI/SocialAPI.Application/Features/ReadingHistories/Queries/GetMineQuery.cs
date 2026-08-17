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

namespace SocialAPI.Application.Features.ReadingHistories.Queries
{
    public class GetMineQuery : IRequest<ReadingHistoryPageDto>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetMineQueryHandler : IRequestHandler<GetMineQuery, ReadingHistoryPageDto>
    {
        private readonly IReadingHistoryRepository _repository;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public GetMineQueryHandler(IReadingHistoryRepository repository, IMissionProgressNotifier missionProgressNotifier)
        {
            _repository = repository;
            _missionProgressNotifier = missionProgressNotifier;
        }

        public async Task<ReadingHistoryPageDto> Handle(GetMineQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var (items, totalCount) = await _repository.GetByUserAsync(request.UserId, page, pageSize);
            return new ReadingHistoryPageDto
            {
                Items = items.Select(Map).ToList(),
                Total = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static ReadingHistoryDto Map(ReadingHistory item) => new()
        {
            Id = item.Id,
            ComicId = item.ComicId,
            ChapterId = item.ChapterId,
            ReadAt = item.ReadAt
        };
    }
}
