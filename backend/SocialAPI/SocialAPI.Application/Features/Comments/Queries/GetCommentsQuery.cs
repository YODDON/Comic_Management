using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using SocialAPI.DTOs;
using SocialAPI.Entities;
using SocialAPI.Interfaces;

namespace SocialAPI.Application.Features.Comments.Queries
{
    public class GetCommentsQuery : IRequest<PagedResult<CommentDto>>
    {
        public Guid ComicId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, PagedResult<CommentDto>>
    {
        private readonly ICommentRepository _repository;
        private readonly IComicValidator _comicValidator;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public GetCommentsQueryHandler(ICommentRepository repository, IComicValidator comicValidator, IMissionProgressNotifier missionProgressNotifier)
        {
            _repository = repository;
            _comicValidator = comicValidator;
            _missionProgressNotifier = missionProgressNotifier;
        }

        public async Task<PagedResult<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
        {
            var totalCount = await _repository.GetCommentsCountAsync(request.ComicId);
            var comments = await _repository.GetCommentsAsync(request.ComicId, (request.Page - 1) * request.PageSize, request.PageSize);

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

            return new PagedResult<CommentDto>(commentDtos, totalCount, request.Page, request.PageSize);
        }
    }
}
