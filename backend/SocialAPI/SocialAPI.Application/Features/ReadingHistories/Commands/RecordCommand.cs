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

namespace SocialAPI.Application.Features.ReadingHistories.Commands
{
    public class RecordCommand : IRequest<ReadingHistoryDto>
    {
        public int NumericUserId { get; set; }
        public Guid UserId { get; set; }
        public RecordReadingHistoryDto Request { get; set; }
    }

    public class RecordCommandHandler : IRequestHandler<RecordCommand, ReadingHistoryDto>
    {
        private readonly IReadingHistoryRepository _repository;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public RecordCommandHandler(IReadingHistoryRepository repository, IMissionProgressNotifier missionProgressNotifier)
        {
            _repository = repository;
            _missionProgressNotifier = missionProgressNotifier;
        }

        public async Task<ReadingHistoryDto> Handle(RecordCommand request, CancellationToken cancellationToken)
        {
            var item = await _repository.GetForUpdateAsync(request.UserId, request.Request.ComicId);
            if (item is null)
            {
                item = new ReadingHistory { UserId = request.UserId, ComicId = request.Request.ComicId };
                _repository.Add(item);
            }

            item.ChapterId = request.Request.ChapterId;
            item.ReadAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _missionProgressNotifier.RecordAsync(
                request.NumericUserId, MissionType.ReadChapter, request.Request.ChapterId, item.ReadAt);
            await _repository.SaveChangesAsync();
            return Map(item);
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
