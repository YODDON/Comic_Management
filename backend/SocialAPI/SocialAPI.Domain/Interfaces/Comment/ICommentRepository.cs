using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.Entities;

namespace SocialAPI.Interfaces
{
    public interface ICommentRepository
    {
        Task<int> GetCommentsCountAsync(Guid comicId);
        Task<List<Comment>> GetCommentsAsync(Guid comicId, int skip, int take);
        Task<List<Comment>> GetRepliesAsync(List<Guid> ParentCommentIds);
        Task<Comment?> GetByIdAsync(Guid id);
        Task<List<Comment>> GetRepliesByParentCommentIdAsync(Guid ParentCommentId);
        Task AddAsync(Comment comment);
        void Remove(Comment comment);
        void RemoveRange(IEnumerable<Comment> comments);
        Task SaveChangesAsync();
    }
}
