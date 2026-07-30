using System;
using System.Threading.Tasks;
using ChapterAPI.DTOs;
using SharedKernel.Responses;

namespace ChapterAPI.Interfaces
{
    public interface IChapterService
    {
        Task<ApiResponse<PagedResult<ChapterSummaryDto>>> GetChaptersAsync(Guid? comicId, string? search, string? status, int pageNumber, int pageSize, int? currentUserId);
        Task<ApiResponse<ChapterDetailDto>> GetChapterDetailAsync(Guid id, bool includePages, int? currentUserId, bool isAdminOrAuthor = false);
        Task<ApiResponse<ChapterSummaryDto>> CreateChapterAsync(CreateChapterRequestDto request);
        Task<ApiResponse<ChapterSummaryDto>> UpdateChapterAsync(Guid id, UpdateChapterRequestDto request);
        Task<ApiResponse<bool>> DeleteChapterAsync(Guid id);
        Task<ApiResponse<List<ChapterPageDto>>> AddPagesBulkAsync(Guid chapterId, List<Microsoft.AspNetCore.Http.IFormFile> files);
        Task<ApiResponse<List<ChapterPageDto>>> AddPagesByUrlsAsync(Guid chapterId, List<string> urls);
        Task<ApiResponse<List<ChapterPageDto>>> GetChapterPagesAsync(Guid id, int? currentUserId, bool isAdminOrAuthor);
        Task<ApiResponse<List<ChapterPageDto>>> GetChapterPagesBySlugAsync(Guid comicId, string slug, int? currentUserId, bool isAdminOrAuthor);
        Task<ApiResponse<bool>> ReorderPagesAsync(Guid chapterId, List<ReorderPageDto> request);
        Task<ApiResponse<bool>> DeletePageAsync(Guid chapterId, Guid pageId);
        Task<ApiResponse<int>> DeletePagesAsync(Guid chapterId, List<Guid> pageIds);
    }
}
