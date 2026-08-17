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
    public class AddPagesByUrlsCommand : IRequest<ApiResponse<List<ChapterPageDto>>>
    {
        public Guid ChapterId { get; set; }
        public List<string> Urls { get; set; }
    }

    public class AddPagesByUrlsCommandHandler : IRequestHandler<AddPagesByUrlsCommand, ApiResponse<List<ChapterPageDto>>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public AddPagesByUrlsCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<List<ChapterPageDto>>> Handle(AddPagesByUrlsCommand request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.ChapterId, false);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<List<ChapterPageDto>>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<List<ChapterPageDto>>(null, "Rejected comics are read-only. Chapter pages cannot be added.", 409);

            var validUrls = request.Urls
                .Select(url => url?.Trim())
                .Where(url => !string.IsNullOrWhiteSpace(url)
                    && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!validUrls.Any())
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "No valid image URLs provided.", 400);
            }

            var maxPageNumber = await _repository.GetMaxPageNumberAsync(request.ChapterId);
            var newPages = validUrls.Select((url, index) => new ChapterAPI.Entities.ChapterPage
            {
                ChapterId = request.ChapterId,
                PageNumber = maxPageNumber + index + 1,
                ImageUrl = url!
            }).ToList();

            await _repository.AddChapterPagesAsync(newPages);
            var dtos = _mapper.Map<List<ChapterPageDto>>(newPages);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages added from URLs successfully.");
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
