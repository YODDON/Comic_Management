using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SharedKernel.Responses;
using System.Text.Json;
using UserAPI.Protos;
using SharedKernel.Enums;
using ChapterAPI.Protos;

namespace ComicAPI.Application.Services
{
    public class ComicService : IComicService
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ChapterGrpc.ChapterGrpcClient _chapterServiceClient;
        private readonly IDistributedCache _cache;

        public ComicService(
            IComicRepository comicRepository,
            IMapper mapper,
            UserService.UserServiceClient userServiceClient,
            ICloudinaryService cloudinaryService,
            ChapterGrpc.ChapterGrpcClient chapterServiceClient,
            IDistributedCache cache)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
            _cloudinaryService = cloudinaryService;
            _chapterServiceClient = chapterServiceClient;
            _cache = cache;
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetComicsAsync(int pageNumber, int pageSize, string? search, List<Guid>? categoryIds, ComicStatus? status)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _comicRepository.GetComicsAsync(pageNumber, pageSize, search, categoryIds, status);

            var dtos = _mapper.Map<List<ComicSummaryDto>>(items);
            await EnrichComicsWithAuthorNamesAsync(dtos, items);
            await EnrichOutstandingFlagsAsync(dtos);

            var pagedResult = new PagedResult<ComicSummaryDto>(dtos, totalCount, pageNumber, pageSize);

            return new ApiResponse<PagedResult<ComicSummaryDto>>(pagedResult, "Comics retrieved successfully.", 200);
        }

        public async Task<ApiResponse<List<ComicSummaryDto>>> GetHotComicsAsync(int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;
            
            string cacheKey = $"HotComics_{limit}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedDtos = JsonSerializer.Deserialize<List<ComicSummaryDto>>(cachedData);
                if (cachedDtos != null) return new ApiResponse<List<ComicSummaryDto>>(cachedDtos, "Hot comics retrieved from cache.", 200);
            }

            var comics = await _comicRepository.GetHotComicsAsync(limit);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);
            
            var serializedDtos = JsonSerializer.Serialize(dtos);
            await _cache.SetStringAsync(cacheKey, serializedDtos, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

            return new ApiResponse<List<ComicSummaryDto>>(dtos, "Hot comics retrieved.", 200);
        }

        public async Task<ApiResponse<ComicCoverUploadDto>> UploadCoverAsync(IFormFile file)
        {
            var imageUrl = await _cloudinaryService.UploadImageAsync(file);
            return string.IsNullOrWhiteSpace(imageUrl)
                ? ApiResponse<ComicCoverUploadDto>.ErrorResponse("Không thể tải ảnh bìa lên.", 400)
                : new ApiResponse<ComicCoverUploadDto>(
                    new ComicCoverUploadDto { ThumbnailUrl = imageUrl },
                    "Comic cover uploaded successfully.");
        }



        public Task<bool> IncrementViewCountAsync(Guid comicId) =>
            _comicRepository.IncrementViewCountAsync(comicId);

        public async Task<ApiResponse<List<ComicSummaryDto>>> GetOutstandingComicsAsync(int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;
            
            string cacheKey = $"OutstandingComics_{limit}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedDtos = JsonSerializer.Deserialize<List<ComicSummaryDto>>(cachedData);
                if (cachedDtos != null) return new ApiResponse<List<ComicSummaryDto>>(cachedDtos, "Outstanding comics retrieved from cache.", 200);
            }

            var comics = await _comicRepository.GetOutstandingComicsAsync(limit);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);
            
            var serializedDtos = JsonSerializer.Serialize(dtos);
            await _cache.SetStringAsync(cacheKey, serializedDtos, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

            return new ApiResponse<List<ComicSummaryDto>>(dtos, "Outstanding comics retrieved.", 200);
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetOutstandingComicsAsync(
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (comics, totalCount) =
                await _comicRepository.GetOutstandingComicsAsync(pageNumber, pageSize);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);

            var result = new PagedResult<ComicSummaryDto>(
                dtos,
                totalCount,
                pageNumber,
                pageSize);
            return new ApiResponse<PagedResult<ComicSummaryDto>>(
                result,
                "Outstanding comics retrieved.",
                200);
        }

        public async Task<ApiResponse<List<ComicSummaryDto>>> GetLastCompletedComicsAsync(int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;
            
            string cacheKey = $"LastCompletedComics_{limit}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedDtos = JsonSerializer.Deserialize<List<ComicSummaryDto>>(cachedData);
                if (cachedDtos != null) return new ApiResponse<List<ComicSummaryDto>>(cachedDtos, "Last completed comics retrieved from cache.", 200);
            }

            var comics = await _comicRepository.GetLastCompletedComicsAsync(limit);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);
            
            var serializedDtos = JsonSerializer.Serialize(dtos);
            await _cache.SetStringAsync(cacheKey, serializedDtos, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

            return new ApiResponse<List<ComicSummaryDto>>(dtos, "Last completed comics retrieved.", 200);
        }

        public async Task<ApiResponse<ComicDetailDto>> GetComicBySlugAsync(string slug)
        {
            var comic = await _comicRepository.GetComicBySlugAsync(slug);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = await _comicRepository.IsComicOutstandingAsync(comic.Id);

            if (!string.IsNullOrWhiteSpace(comic.Author))
            {
                dto.AuthorName = comic.Author.Trim();
            }
            // Fall back to the owner account name for legacy comics without an author value.
            else
            try
            {
                var request = new GetUsersByIdsRequest();
                request.Ids.Add(comic.OwnerId.ToString());

                var response = await _userServiceClient.GetUsersByIdsAsync(request);
                var user = response.Users.FirstOrDefault();
                dto.AuthorName = user != null ? user.Username : "Unknown";
            }
            catch
            {
                dto.AuthorName = "Unknown";
            }

            try
            {
                var chapterResponse = await _chapterServiceClient.GetChapterCountAsync(
                    new GetChapterCountRequest { ComicId = comic.Id.ToString() });
                dto.ChapterCount = chapterResponse.Count;
            }
            catch
            {
                // Comic detail remains available when ChapterAPI is temporarily unavailable.
                dto.ChapterCount = 0;
            }

            return new ApiResponse<ComicDetailDto>(dto, "Comic retrieved successfully.", 200);
        }

        public async Task<ApiResponse<ComicDetailDto>> GetComicByIdAsync(Guid id)
        {
            var comic = await _comicRepository.GetComicByIdAsync(id);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            return await GetComicBySlugAsync(comic.Slug);
        }

        public async Task<ApiResponse<ComicDetailDto>> CreateComicAsync(CreateComicRequestDto request, int userId)
        {
            string coverUrl = request.CoverImage != null
                ? await _cloudinaryService.UploadImageAsync(request.CoverImage)
                : request.CoverUrl!.Trim();

            var comic = new Comic
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                ThumbnailUrl = coverUrl,
                OwnerId = userId,
                Author = request.Author ?? string.Empty,
                UnitPrice = request.UnitPrice,
                SalaryType = request.SalaryType?.ToString() ?? "0",
                Status = ComicStatus.Ongoing,
                ViewCount = 0,
                Slug = !string.IsNullOrWhiteSpace(request.Slug) 
                    ? request.Slug 
                    : SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Title)
            };

            if (request.CategoryIds != null && request.CategoryIds.Any())
            {
                comic.ComicCategories = request.CategoryIds.Select(id => new ComicCategory { CategoryId = id }).ToList();
            }

            await _comicRepository.AddComicAsync(comic);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic created successfully.", 201);
        }

        public async Task<ApiResponse<ComicDetailDto>> UpdateComicAsync(Guid comicId, UpdateComicRequestDto request, int userId, bool isAdmin)
        {
            var comic = await _comicRepository.GetComicByIdAsync(comicId);
            if (comic == null) return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics are read-only and cannot be edited.", 409);
            }

            if (!isAdmin && comic.OwnerId != userId)
            {
                return new ApiResponse<ComicDetailDto>(null, "You are not authorized to update this comic.", 403);
            }

            comic.Title = request.Title;
            comic.Slug = await GenerateUniqueSlugAsync(request.Title, comic.Id);
            comic.Description = request.Description;
            comic.Author = request.Author?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
            {
                comic.ThumbnailUrl = request.ThumbnailUrl.Trim();
            }
            if (isAdmin)
            {
                if (request.Status.HasValue)
                {
                    comic.Status = request.Status.Value;
                }
            }
            // Optionally update slug if title changes, but often we keep slug constant. Let's keep it constant for SEO.

            var requestedCategoryIds = (request.CategoryIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            await _comicRepository.UpdateComicAsync(comic, requestedCategoryIds);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic updated successfully.", 200);
        }

        public async Task<ApiResponse<ComicDetailDto>> UpdateComicStatusAsync(Guid comicId, UpdateComicStatusRequestDto request)
        {
            var comic = await _comicRepository.GetComicByIdAsync(comicId);
            if (comic == null) return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);

            if (comic.Status == ComicStatus.Dropped && request.Status != ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot be approved again.", 409);
            }

            comic.Status = request.Status;
            await _comicRepository.UpdateComicAsync(comic);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic status updated successfully.", 200);
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetMyComicsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (comics, totalCount) = await _comicRepository.GetComicsByOwnerAsync(userId, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);

            var pagedResult = new PagedResult<ComicSummaryDto>(dtos, totalCount, pageNumber, pageSize);
            return new ApiResponse<PagedResult<ComicSummaryDto>>(pagedResult, "My comics retrieved.", 200);
        }

        public async Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetPurchasedComicsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            List<Guid> purchasedComicIds;
            try
            {
                var response = await _chapterServiceClient.GetPurchasedComicIdsAsync(
                    new GetPurchasedComicIdsRequest { UserId = userId });
                purchasedComicIds = response.ComicIds
                    .Select(id => Guid.TryParse(id, out var comicId) ? comicId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();
            }
            catch
            {
                return ApiResponse<PagedResult<ComicSummaryDto>>.ErrorResponse(
                    "Could not retrieve purchased comics from ChapterAPI.",
                    503);
            }

            if (!purchasedComicIds.Any())
            {
                var emptyResult = new PagedResult<ComicSummaryDto>(new List<ComicSummaryDto>(), 0, pageNumber, pageSize);
                return new ApiResponse<PagedResult<ComicSummaryDto>>(emptyResult, "Purchased comics retrieved.", 200);
            }

            var (comics, totalCount) = await _comicRepository.GetComicsByIdsAsync(purchasedComicIds, pageNumber, pageSize);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await EnrichComicsWithAuthorNamesAsync(dtos, comics);

            var pagedResult = new PagedResult<ComicSummaryDto>(dtos, totalCount, pageNumber, pageSize);
            return new ApiResponse<PagedResult<ComicSummaryDto>>(pagedResult, "Purchased comics retrieved.", 200);
        }

        public async Task<ApiResponse<ComicDetailDto>> AddOutstandingComicAsync(CreateOutstandingRequestDto request)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.ComicId);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot be marked as outstanding.", 409);
            }

            var isOutstanding = await _comicRepository.IsComicOutstandingAsync(request.ComicId);
            if (isOutstanding)
            {
                var existingDto = _mapper.Map<ComicDetailDto>(comic);
                return new ApiResponse<ComicDetailDto>(existingDto, "Comic is already outstanding.", 200);
            }

            var outstanding = new Outstanding
            {
                ComicId = request.ComicId,
                Priority = request.Priority,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = request.EndDate ?? DateTime.UtcNow.AddDays(7)
            };

            await _comicRepository.AddOutstandingAsync(outstanding);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = true;
            var summary = _mapper.Map<ComicSummaryDto>(comic);
            await EnrichComicsWithAuthorNamesAsync(new List<ComicSummaryDto> { summary }, new List<Comic> { comic });
            dto.AuthorName = summary.AuthorName;

            return new ApiResponse<ComicDetailDto>(dto, "Comic added to outstandings.", 201);
        }

        public async Task<ApiResponse<ComicDetailDto>> ToggleOutstandingComicAsync(CreateOutstandingRequestDto request)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.ComicId);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot change outstanding status.", 409);
            }

            var isOutstanding = await _comicRepository.IsComicOutstandingAsync(request.ComicId);
            if (isOutstanding)
            {
                await _comicRepository.RemoveOutstandingAsync(request.ComicId);
            }
            else
            {
                await _comicRepository.AddOutstandingAsync(new Outstanding
                {
                    ComicId = request.ComicId,
                    Priority = request.Priority,
                    StartDate = request.StartDate ?? DateTime.UtcNow,
                    EndDate = request.EndDate ?? DateTime.UtcNow.AddDays(7)
                });
            }

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = !isOutstanding;
            return new ApiResponse<ComicDetailDto>(
                dto,
                dto.IsOutstanding ? "Comic marked as outstanding." : "Comic removed from outstandings.",
                200);
        }

        private async Task EnrichOutstandingFlagsAsync(List<ComicSummaryDto> dtos)
        {
            foreach (var dto in dtos)
            {
                dto.IsOutstanding = await _comicRepository.IsComicOutstandingAsync(dto.Id);
            }
        }

        private async Task EnrichComicsWithAuthorNamesAsync(List<ComicSummaryDto> dtos, List<Comic> comics)
        {
            var comicsWithoutAuthor = comics
                .Where(comic => string.IsNullOrWhiteSpace(comic.Author))
                .ToList();

            foreach (var dto in dtos)
            {
                var comic = comics.First(item => item.Id == dto.Id);
                if (!string.IsNullOrWhiteSpace(comic.Author))
                {
                    dto.AuthorName = comic.Author.Trim();
                }
            }

            var ownerIds = comicsWithoutAuthor.Select(i => i.OwnerId.ToString()).Distinct().ToList();
            if (!ownerIds.Any()) return;

            try
            {
                var request = new GetUsersByIdsRequest();
                request.Ids.AddRange(ownerIds);

                var response = await _userServiceClient.GetUsersByIdsAsync(request);
                var userDict = response.Users.ToDictionary(u => u.Id, u => u.Username);

                foreach (var dto in dtos)
                {
                    var originalComic = comics.First(c => c.Id == dto.Id);
                    if (!string.IsNullOrWhiteSpace(originalComic.Author)) continue;
                    if (userDict.TryGetValue(originalComic.OwnerId.ToString(), out var username))
                    {
                        dto.AuthorName = username;
                    }
                    else
                    {
                        dto.AuthorName = "Unknown";
                    }
                }
            }
            catch
            {
                foreach (var dto in dtos)
                {
                    var originalComic = comics.First(c => c.Id == dto.Id);
                    if (string.IsNullOrWhiteSpace(originalComic.Author)) dto.AuthorName = "Unknown";
                }
            }
        }

        private async Task<string> GenerateUniqueSlugAsync(string title, Guid excludedComicId)
        {
            var baseSlug = SharedKernel.Utilities.SlugGenerator.GenerateSlug(title);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = $"truyen-{excludedComicId.ToString("N")[..8]}";
            }

            var candidate = baseSlug;
            var suffix = 2;
            while (await _comicRepository.SlugExistsAsync(candidate, excludedComicId))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return candidate;
        }
    }
}
