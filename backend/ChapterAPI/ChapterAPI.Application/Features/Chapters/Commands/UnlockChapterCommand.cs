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
    public class UnlockChapterCommand : IRequest<(bool Success, bool AlreadyPurchased)>
    {
        public int UserId { get; set; }
        public Guid ChapterId { get; set; }
    }

    public class UnlockChapterCommandHandler : IRequestHandler<UnlockChapterCommand, (bool Success, bool AlreadyPurchased)>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public UnlockChapterCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<(bool Success, bool AlreadyPurchased)> Handle(UnlockChapterCommand request, CancellationToken cancellationToken)
        {
            var alreadyPurchased = await _repository.HasUserPurchasedChapterAsync(request.UserId, request.ChapterId);
            if (alreadyPurchased)
            {
                await _missionProgressNotifier.RecordAsync(request.UserId, MissionType.PurchaseChapter, request.ChapterId, DateTime.UtcNow);
                return (true, true);
            }

            var success = await _repository.UnlockChapterAsync(request.UserId, request.ChapterId);
            if (success)
            {
                await _missionProgressNotifier.RecordAsync(request.UserId, MissionType.PurchaseChapter, request.ChapterId, DateTime.UtcNow);
                await _repository.SaveChangesAsync();
            }
            return (success, false);
        }
    }
}
