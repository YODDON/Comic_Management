using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using SharedKernel.Responses;
using SharedKernel.Enums;

namespace ComicAPI.Application.Interfaces
{
    public interface IComicService
    {
        Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetComicsAsync(int pageNumber, int pageSize, string? search, List<Guid>? categoryIds, ComicStatus? status);
        Task<ApiResponse<List<ComicSummaryDto>>> GetHotComicsAsync(int limit);
        Task<ApiResponse<List<ComicSummaryDto>>> GetOutstandingComicsAsync(int limit);
        Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetOutstandingComicsAsync(int pageNumber, int pageSize);
        Task<ApiResponse<List<ComicSummaryDto>>> GetLastCompletedComicsAsync(int limit);
        Task<ApiResponse<ComicDetailDto>> GetComicBySlugAsync(string slug);
        Task<ApiResponse<ComicDetailDto>> GetComicByIdAsync(Guid id);
        Task<ApiResponse<ComicDetailDto>> CreateComicAsync(CreateComicRequestDto request, int userId);
        Task<ApiResponse<ComicDetailDto>> UpdateComicAsync(System.Guid comicId, UpdateComicRequestDto request, int userId, bool isAdmin);
        Task<ApiResponse<ComicDetailDto>> UpdateComicStatusAsync(System.Guid comicId, UpdateComicStatusRequestDto request);
        Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetMyComicsAsync(int userId, int pageNumber, int pageSize);
        Task<ApiResponse<PagedResult<ComicSummaryDto>>> GetPurchasedComicsAsync(int userId, int pageNumber, int pageSize);
        Task<ApiResponse<ComicDetailDto>> AddOutstandingComicAsync(CreateOutstandingRequestDto request);
        Task<ApiResponse<ComicDetailDto>> ToggleOutstandingComicAsync(CreateOutstandingRequestDto request);
        Task<ApiResponse<ComicCoverUploadDto>> UploadCoverAsync(Microsoft.AspNetCore.Http.IFormFile file);

        Task<bool> IncrementViewCountAsync(Guid comicId);
    }
}
