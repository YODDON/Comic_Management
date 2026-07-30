using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ChapterAPI.DTOs;
using ChapterAPI.Interfaces;
using SharedKernel.Responses;
using Grpc.Core;

namespace ChapterAPI.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepository _repository;
        private readonly IMapper _mapper;
        private readonly ChapterAPI.Protos.ComicGrpc.ComicGrpcClient _comicServiceClient;
        private readonly ICloudinaryService _cloudinaryService;

        public ChapterService(
            IChapterRepository repository, 
            IMapper mapper, 
            ChapterAPI.Protos.ComicGrpc.ComicGrpcClient comicServiceClient,
            ICloudinaryService cloudinaryService)
        {
            _repository = repository;
            _mapper = mapper;
            _comicServiceClient = comicServiceClient;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<PagedResult<ChapterSummaryDto>>> GetChaptersAsync(
            Guid? comicId,
            string? search,
            string? status,
            int pageNumber,
            int pageSize,
            int? currentUserId)
        {
            var (items, totalCount) = await _repository.GetChaptersAsync(comicId, search, status, pageNumber, pageSize);

            var dtos = _mapper.Map<List<ChapterSummaryDto>>(items);
            if (currentUserId.HasValue)
            {
                var paidChapterIds = items
                    .Where(chapter => chapter.UnitPrice > 0)
                    .Select(chapter => chapter.Id)
                    .ToList();
                var purchasedChapterIds = await _repository.GetPurchasedChapterIdsAsync(
                    currentUserId.Value, paidChapterIds);
                foreach (var dto in dtos)
                {
                    dto.IsPurchased = purchasedChapterIds.Contains(dto.Id);
                }
            }

            var pagedResult = new PagedResult<ChapterSummaryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };

            return new ApiResponse<PagedResult<ChapterSummaryDto>>(pagedResult, "Chapters retrieved successfully.");
        }

        public async Task<ApiResponse<ChapterDetailDto>> GetChapterDetailAsync(Guid id, bool includePages, int? currentUserId, bool isAdminOrAuthor = false)
        {
            var chapter = await _repository.GetChapterByIdAsync(id, includePages);
            if (chapter == null)
            {
                return new ApiResponse<ChapterDetailDto>(null, "Chapter not found.", 404);
            }

            if (!isAdminOrAuthor && !string.Equals(chapter.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<ChapterDetailDto>(null, "Chapter not found.", 404);
            }

            var dto = _mapper.Map<ChapterDetailDto>(chapter);

            if (includePages)
            {
                if (chapter.UnitPrice > 0 && !isAdminOrAuthor)
                {
                    if (currentUserId == null)
                    {
                        return new ApiResponse<ChapterDetailDto>(null, "Authentication required to view VIP chapter.", 401);
                    }

                    var hasPurchased = await _repository.HasUserPurchasedChapterAsync(currentUserId.Value, id);
                    if (!hasPurchased)
                    {
                        dto.ChapterPages = new List<ChapterPageDto>();
                        return new ApiResponse<ChapterDetailDto>(dto, "Bạn cần mua chapter này.", 403);
                    }
                }
            }

            return new ApiResponse<ChapterDetailDto>(dto, "Chapter details retrieved successfully.");
        }

        public async Task<ApiResponse<ChapterSummaryDto>> CreateChapterAsync(CreateChapterRequestDto request)
        {
            ChapterAPI.Protos.CheckComicExistsResponse grpcResponse;
            try
            {
                grpcResponse = await _comicServiceClient.CheckComicExistsAsync(new ChapterAPI.Protos.CheckComicExistsRequest
                {
                    ComicId = request.ComicId.ToString()
                });
            }
            catch (RpcException)
            {
                return ApiResponse<ChapterSummaryDto>.ErrorResponse(
                    "ComicAPI gRPC is unavailable. Please make sure the ComicAPI HTTPS profile is running on port 7024.",
                    503);
            }

            if (!grpcResponse.Exists)
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Comic not found.", 404);
            }

            if (string.Equals(grpcResponse.Status, "Dropped", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Rejected comics are read-only. Chapters cannot be added.", 409);
            }

            var slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Title);
            var isSlugUnique = await _repository.IsSlugUniqueAsync(request.ComicId, slug);
            if (!isSlugUnique)
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Chapter with this title already exists in the comic.", 409);
            }

            var chapter = new ChapterAPI.Entities.Chapter
            {
                ComicId = request.ComicId,
                Title = request.Title,
                Slug = slug,
                ChapterNumber = request.ChapterNumber,
                UnitPrice = request.UnitPrice,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddChapterAsync(chapter);

            var dto = _mapper.Map<ChapterSummaryDto>(chapter);
            return new ApiResponse<ChapterSummaryDto>(dto, "Chapter created successfully.");
        }

        public async Task<ApiResponse<ChapterSummaryDto>> UpdateChapterAsync(Guid id, UpdateChapterRequestDto request)
        {
            var chapter = await _repository.GetChapterByIdAsync(id, false);
            if (chapter == null)
            {
                return new ApiResponse<ChapterSummaryDto>(null, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<ChapterSummaryDto>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<ChapterSummaryDto>(null, "Rejected comics are read-only. Chapters cannot be edited.", 409);

            var slug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Title);
            if (chapter.Slug != slug)
            {
                var isSlugUnique = await _repository.IsSlugUniqueAsync(chapter.ComicId, slug);
                if (!isSlugUnique)
                {
                    return new ApiResponse<ChapterSummaryDto>(null, "Chapter with this title already exists in the comic.", 409);
                }
            }

            chapter.Title = request.Title;
            chapter.Slug = slug;
            chapter.ChapterNumber = request.ChapterNumber;
            chapter.UnitPrice = request.UnitPrice;
            chapter.Status = request.Status;

            await _repository.UpdateChapterAsync(chapter);

            var dto = _mapper.Map<ChapterSummaryDto>(chapter);
            return new ApiResponse<ChapterSummaryDto>(dto, "Chapter updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteChapterAsync(Guid id)
        {
            var chapter = await _repository.GetChapterByIdAsync(id, false);
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

        public async Task<ApiResponse<List<ChapterPageDto>>> AddPagesBulkAsync(Guid chapterId, List<Microsoft.AspNetCore.Http.IFormFile> files)
        {
            var chapter = await _repository.GetChapterByIdAsync(chapterId, false);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<List<ChapterPageDto>>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<List<ChapterPageDto>>(null, "Rejected comics are read-only. Chapter pages cannot be added.", 409);

            var validFiles = files.Where(f => f.Length > 0).ToList();
            if (!validFiles.Any())
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "No valid files provided.", 400);
            }

            var uploadTasks = validFiles.Select(f =>
                _cloudinaryService.UploadImageAsync(f, $"chapters/{chapterId}")).ToList();
            
            var uploadResults = await Task.WhenAll(uploadTasks);

            var maxPageNumber = await _repository.GetMaxPageNumberAsync(chapterId);
            
            var newPages = new List<ChapterAPI.Entities.ChapterPage>();
            for (int i = 0; i < uploadResults.Length; i++)
            {
                if (!string.IsNullOrEmpty(uploadResults[i]))
                {
                    newPages.Add(new ChapterAPI.Entities.ChapterPage
                    {
                        ChapterId = chapterId,
                        PageNumber = maxPageNumber + i + 1,
                        ImageUrl = uploadResults[i]
                    });
                }
            }

            if (newPages.Any())
            {
                await _repository.AddChapterPagesAsync(newPages);
            }

            var dtos = _mapper.Map<List<ChapterPageDto>>(newPages);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages uploaded successfully.");
        }

        public async Task<ApiResponse<List<ChapterPageDto>>> AddPagesByUrlsAsync(Guid chapterId, List<string> urls)
        {
            var chapter = await _repository.GetChapterByIdAsync(chapterId, false);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<List<ChapterPageDto>>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<List<ChapterPageDto>>(null, "Rejected comics are read-only. Chapter pages cannot be added.", 409);

            var validUrls = urls
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

            var maxPageNumber = await _repository.GetMaxPageNumberAsync(chapterId);
            var newPages = validUrls.Select((url, index) => new ChapterAPI.Entities.ChapterPage
            {
                ChapterId = chapterId,
                PageNumber = maxPageNumber + index + 1,
                ImageUrl = url!
            }).ToList();

            await _repository.AddChapterPagesAsync(newPages);
            var dtos = _mapper.Map<List<ChapterPageDto>>(newPages);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages added from URLs successfully.");
        }

        public async Task<ApiResponse<List<ChapterPageDto>>> GetChapterPagesAsync(Guid id, int? currentUserId, bool isAdminOrAuthor)
        {
            var chapter = await _repository.GetChapterByIdAsync(id, true);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (!isAdminOrAuthor && !string.Equals(chapter.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (chapter.UnitPrice > 0 && !isAdminOrAuthor)
            {
                if (currentUserId == null)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần đăng nhập để xem chapter này.", 401);
                }

                var hasPurchased = await _repository.HasUserPurchasedChapterAsync(currentUserId.Value, id);
                if (!hasPurchased)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần mua chapter này.", 403);
                }
            }

            var dtos = _mapper.Map<List<ChapterPageDto>>(chapter.ChapterPages);
            if (!isAdminOrAuthor) await RecordComicReadAsync(chapter.ComicId);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages retrieved successfully.");
        }

        public async Task<ApiResponse<List<ChapterPageDto>>> GetChapterPagesBySlugAsync(Guid comicId, string slug, int? currentUserId, bool isAdminOrAuthor)
        {
            var chapter = await _repository.GetChapterBySlugAsync(comicId, slug);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (!isAdminOrAuthor && !string.Equals(chapter.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            chapter = await _repository.GetChapterByIdAsync(chapter.Id, true);
            if (chapter == null)
            {
                return new ApiResponse<List<ChapterPageDto>>(null, "Chapter not found.", 404);
            }

            if (chapter.UnitPrice > 0 && !isAdminOrAuthor)
            {
                if (currentUserId == null)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần đăng nhập để xem chapter này.", 401);
                }

                var hasPurchased = await _repository.HasUserPurchasedChapterAsync(currentUserId.Value, chapter.Id);
                if (!hasPurchased)
                {
                    return new ApiResponse<List<ChapterPageDto>>(null, "Bạn cần mua chapter này.", 403);
                }
            }

            var dtos = _mapper.Map<List<ChapterPageDto>>(chapter.ChapterPages);
            if (!isAdminOrAuthor) await RecordComicReadAsync(chapter.ComicId);
            return new ApiResponse<List<ChapterPageDto>>(dtos, "Pages retrieved successfully.");
        }

        private async Task RecordComicReadAsync(Guid comicId)
        {
            try
            {
                await _comicServiceClient.IncrementComicViewAsync(new ChapterAPI.Protos.IncrementComicViewRequest
                {
                    ComicId = comicId.ToString()
                });
            }
            catch (RpcException)
            {
                // Reading remains available if ComicAPI cannot record analytics temporarily.
            }
        }

        private async Task<bool?> IsRejectedComicAsync(Guid comicId)
        {
            try
            {
                var response = await _comicServiceClient.CheckComicExistsAsync(
                    new ChapterAPI.Protos.CheckComicExistsRequest { ComicId = comicId.ToString() });
                return response.Exists
                    && string.Equals(response.Status, "Dropped", StringComparison.OrdinalIgnoreCase);
            }
            catch (RpcException)
            {
                return null;
            }
        }

        public async Task<ApiResponse<bool>> ReorderPagesAsync(Guid chapterId, List<ReorderPageDto> request)
        {
            var chapter = await _repository.GetChapterByIdAsync(chapterId, true);
            if (chapter == null)
            {
                return new ApiResponse<bool>(false, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<bool>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<bool>(false, "Rejected comics are read-only. Chapter pages cannot be reordered.", 409);

            var pagesToUpdate = new List<ChapterAPI.Entities.ChapterPage>();
            var newOrderNumbers = new HashSet<int>();

            foreach (var item in request)
            {
                var page = chapter.ChapterPages.FirstOrDefault(p => p.Id == item.PageId);
                if (page == null)
                {
                    return new ApiResponse<bool>(false, $"PageId {item.PageId} does not belong to this chapter.", 400);
                }

                if (!newOrderNumbers.Add(item.NewOrder))
                {
                    return new ApiResponse<bool>(false, $"Duplicate pageNumber {item.NewOrder} in request.", 400);
                }

                page.PageNumber = item.NewOrder;
                pagesToUpdate.Add(page);
            }

            // Also ensure that across ALL pages in chapter, there are no duplicates.
            // Since we might only update a subset, we must verify the entire set.
            var allFinalNumbers = new HashSet<int>();
            foreach (var p in chapter.ChapterPages)
            {
                if (!allFinalNumbers.Add(p.PageNumber))
                {
                    return new ApiResponse<bool>(false, $"Reordering results in duplicate pageNumber {p.PageNumber}.", 400);
                }
            }

            if (pagesToUpdate.Any())
            {
                await _repository.UpdateChapterPagesAsync(pagesToUpdate);
            }

            return new ApiResponse<bool>(true, "Pages reordered successfully.");
        }

        public async Task<ApiResponse<bool>> DeletePageAsync(Guid chapterId, Guid pageId)
        {
            var chapter = await _repository.GetChapterByIdAsync(chapterId, true);
            if (chapter == null)
            {
                return new ApiResponse<bool>(false, "Chapter not found.", 404);
            }

            var rejected = await IsRejectedComicAsync(chapter.ComicId);
            if (rejected is null) return ApiResponse<bool>.ErrorResponse("ComicAPI gRPC is unavailable.", 503);
            if (rejected.Value) return new ApiResponse<bool>(false, "Rejected comics are read-only. Chapter pages cannot be deleted.", 409);

            var page = chapter.ChapterPages.FirstOrDefault(p => p.Id == pageId);
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

        public async Task<ApiResponse<int>> DeletePagesAsync(Guid chapterId, List<Guid> pageIds)
        {
            var distinctIds = pageIds.Distinct().ToHashSet();
            if (distinctIds.Count == 0)
            {
                return new ApiResponse<int>(0, "At least one page must be selected.", 400);
            }

            var chapter = await _repository.GetChapterByIdAsync(chapterId, true);
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
                if (segments[startIndex].StartsWith('v') &&
                    segments[startIndex].Length > 1 &&
                    char.IsDigit(segments[startIndex][1]))
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
