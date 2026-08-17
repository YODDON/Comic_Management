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
    public class DeleteChapterCommand : IRequest<ApiResponse<bool>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteChapterCommandHandler : IRequestHandler<DeleteChapterCommand, ApiResponse<bool>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public DeleteChapterCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteChapterCommand request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.Id, false);
            if (chapter == null)
            {
                return new ApiResponse<bool>(false, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<bool>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<bool>(false, "Rejected comics are read-only. Chapters cannot be deleted.", 409);

            await _repository.DeleteChapterAsync(chapter);
            return new ApiResponse<bool>(true, "Chapter deleted successfully.");
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
