using System;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;

namespace SocialAPI.Interfaces
{
    public interface ICommentService
    {
        Task<PagedResult<CommentDto>> GetCommentsAsync(Guid comicId, int page, int pageSize);
        Task<CommentDto?> CreateCommentAsync(Guid userId, CreateCommentDto dto);
        Task<bool> DeleteCommentAsync(Guid id, Guid userId, bool isAdmin);
    }
}
