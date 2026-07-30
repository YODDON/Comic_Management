using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly SocialDbContext _context;

        public CommentRepository(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetCommentsCountAsync(Guid comicId)
        {
            return await _context.Comments
                .Where(c => c.ComicId == comicId && c.ParentCommentId == null)
                .CountAsync();
        }

        public async Task<List<Comment>> GetCommentsAsync(Guid comicId, int skip, int take)
        {
            return await _context.Comments
                .Where(c => c.ComicId == comicId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetRepliesAsync(List<Guid> ParentCommentIds)
        {
            return await _context.Comments
                .Where(c => c.ParentCommentId != null && ParentCommentIds.Contains(c.ParentCommentId.Value))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment?> GetByIdAsync(Guid id)
        {
            return await _context.Comments.FindAsync(id);
        }

        public async Task<List<Comment>> GetRepliesByParentCommentIdAsync(Guid ParentCommentId)
        {
            return await _context.Comments.Where(c => c.ParentCommentId == ParentCommentId).ToListAsync();
        }

        public async Task AddAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
        }

        public void Remove(Comment comment)
        {
            _context.Comments.Remove(comment);
        }

        public void RemoveRange(IEnumerable<Comment> comments)
        {
            _context.Comments.RemoveRange(comments);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
