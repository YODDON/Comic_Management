using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Responses;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _repository;
        private readonly IComicValidator _comicValidator;

        public CommentService(ICommentRepository repository, IComicValidator comicValidator)
        {
            _repository = repository;
            _comicValidator = comicValidator;
        }

        public async Task<PagedResult<CommentDto>> GetCommentsAsync(Guid comicId, int page, int pageSize)
        {
            var totalCount = await _repository.GetCommentsCountAsync(comicId);
            var comments = await _repository.GetCommentsAsync(comicId, (page - 1) * pageSize, pageSize);

            var commentDtos = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                ComicId = c.ComicId,
                Content = c.Content,
                ParentCommentId = c.ParentCommentId,
                CreatedAt = c.CreatedAt
            }).ToList();

            var ParentCommentIds = comments.Select(c => c.Id).ToList();
            var allReplies = await _repository.GetRepliesAsync(ParentCommentIds);

            var repliesLookup = allReplies.GroupBy(c => c.ParentCommentId!.Value).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dto in commentDtos)
            {
                if (repliesLookup.TryGetValue(dto.Id, out var replies))
                {
                    dto.Replies = replies.Select(r => new CommentDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        ComicId = r.ComicId,
                        Content = r.Content,
                        ParentCommentId = r.ParentCommentId,
                        CreatedAt = r.CreatedAt
                    }).ToList();
                }
            }

            return new PagedResult<CommentDto>(commentDtos, totalCount, page, pageSize);
        }

        public async Task<CommentDto?> CreateCommentAsync(Guid userId, CreateCommentDto dto)
        {
            var exists = await _comicValidator.ExistsAsync(dto.ComicId);
            if (!exists)
            {
                return null; // Signals that comic wasn't found
            }

            var comment = new Comment
            {
                UserId = userId,
                ComicId = dto.ComicId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId
            };

            await _repository.AddAsync(comment);
            await _repository.SaveChangesAsync();

            return new CommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                ComicId = comment.ComicId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteCommentAsync(Guid id, Guid userId, bool isAdmin)
        {
            var comment = await _repository.GetByIdAsync(id);
            if (comment == null)
            {
                return false;
            }

            if (comment.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this comment.");
            }

            _repository.Remove(comment);

            var replies = await _repository.GetRepliesByParentCommentIdAsync(id);
            _repository.RemoveRange(replies);

            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
