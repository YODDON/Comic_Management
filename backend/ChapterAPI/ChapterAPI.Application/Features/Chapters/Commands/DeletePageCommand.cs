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
    public class DeletePageCommand : IRequest<ApiResponse<bool>>
    {
        public Guid ChapterId { get; set; }
        public Guid PageId { get; set; }
    }

    public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, ApiResponse<bool>>
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly IComicValidator _comicValidator;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMissionProgressNotifier _missionProgressNotifier;
        private readonly IPublishEndpoint _publishEndpoint;

        public DeletePageCommandHandler(IChapterRepository repository, IMapper mapper, IComicValidator comicValidator, ICloudinaryService cloudinaryService, IMissionProgressNotifier missionProgressNotifier, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _mapper = mapper;
            _comicValidator = comicValidator;
            _cloudinaryService = cloudinaryService;
            _missionProgressNotifier = missionProgressNotifier;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(DeletePageCommand request, CancellationToken cancellationToken)
        {
            var chapter = await _repository.GetChapterByIdAsync(request.ChapterId, true);
            if (chapter == null)
            {
                return new ApiResponse<bool>(false, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<bool>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<bool>(false, "Rejected comics are read-only. Chapter pages cannot be deleted.", 409);

            var page = chapter.ChapterPages.FirstOrDefault(p => p.Id == request.PageId);
            if (page == null)
            {
                return new ApiResponse<bool>(false, "Page not found in this chapter.", 404);
            }

            string publicId = string.Empty;
            if (!string.IsNullOrEmpty(page.ImageUrl))
            {
                try
                {
                    var uri = new Uri(page.ImageUrl);
                    var segments = uri.Segments;
                    
                    int uploadIndex = -1;
                    for(int i = 0; i < segments.Length; i++)
                    {
                        if (segments[i] == "upload/") 
                        {
                            uploadIndex = i;
                            break;
                        }
                    }

                    if (uploadIndex != -1 && uploadIndex + 1 < segments.Length)
                    {
                        int startIndex = uploadIndex + 1;
                        // Skip version if it starts with 'v' and is followed by digits
                        if (segments[startIndex].StartsWith("v") && segments[startIndex].Length > 1 && char.IsDigit(segments[startIndex][1]))
                        {
                            startIndex++;
                        }

                        var publicIdParts = new List<string>();
                        for (int i = startIndex; i < segments.Length; i++)
                        {
                            publicIdParts.Add(segments[i]);
                        }
                        
                        publicId = string.Join("", publicIdParts);
                        int extIndex = publicId.LastIndexOf('.');
                        if (extIndex > 0)
                        {
                            publicId = publicId.Substring(0, extIndex);
                        }

                        publicId = Uri.UnescapeDataString(publicId);
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(publicId))
            {
                await _cloudinaryService.DeleteImageAsync(publicId);
            }

            await _repository.DeleteChapterPageAsync(page);

            return new ApiResponse<bool>(true, "Page deleted successfully.");
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
