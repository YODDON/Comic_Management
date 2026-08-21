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
    public class DeletePagesCommand : IRequest<ApiResponse<int>>
    {
        public Guid ChapterId { get; set; }
        public List<Guid> PageIds { get; set; }
    }

    public class DeletePagesCommandHandler : IRequestHandler<DeletePagesCommand, ApiResponse<int>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public DeletePagesCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<int>> Handle(DeletePagesCommand request, CancellationToken cancellationToken)
        {
            var distinctIds = request.PageIds.Distinct().ToHashSet();
            if (distinctIds.Count == 0)
            {
                return new ApiResponse<int>(0, "At least one page must be selected.", 400);
            }

            var chapter = await _repository.GetChapterByIdAsync(request.ChapterId, true);
            if (chapter == null)
            {
                return new ApiResponse<int>(0, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<int>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<int>(0, "Rejected comics are read-only. Chapter pages cannot be deleted.", 409);

            var selectedPages = chapter.ChapterPages
                .Where(page => distinctIds.Contains(page.Id))
                .ToList();
            if (selectedPages.Count != distinctIds.Count)
            {
                return new ApiResponse<int>(0, "One or more selected pages do not belong to this chapter.", 400);
            }

            await _repository.DeleteChapterPagesAsync(chapter, selectedPages);

            foreach (var page in selectedPages)
            {
                var publicId = ExtractCloudinaryPublicId(page.ImageUrl);
                if (string.IsNullOrEmpty(publicId)) continue;
                try
                {
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch
                {
                    // The database operation is already complete. An unavailable image
                    // provider must not make the API report that the pages were not deleted.
                }
            }

            return new ApiResponse<int>(selectedPages.Count, $"Deleted {selectedPages.Count} pages successfully.");
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

        private static string ExtractCloudinaryPublicId(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;
            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                var uploadIndex = Array.FindIndex(segments, segment => segment == "upload/");
                if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length) return string.Empty;

                var startIndex = uploadIndex + 1;
                if (segments[startIndex].StartsWith('v') && segments[startIndex].Length > 1 && char.IsDigit(segments[startIndex][1]))
                {
                    startIndex++;
                }

                var publicId = string.Concat(segments.Skip(startIndex));
                var extensionIndex = publicId.LastIndexOf('.');
                if (extensionIndex > 0) publicId = publicId[..extensionIndex];
                return Uri.UnescapeDataString(publicId);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
