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

namespace SocialAPI.Application.Features.Comments.Commands
{
    public class CreateCommentCommand : IRequest<CommentDto?>
    {
        public int NumericUserId { get; set; }
        public Guid UserId { get; set; }
        public CreateCommentDto Dto { get; set; }
    }

    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto?>
    {
        private readonly ICommentRepository _repository;
        private readonly IComicValidator _comicValidator;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public CreateCommentCommandHandler(ICommentRepository repository, IComicValidator comicValidator, IMissionProgressNotifier missionProgressNotifier)
        {
            _repository = repository;
            _comicValidator = comicValidator;
            _missionProgressNotifier = missionProgressNotifier;
        }

        public async Task<CommentDto?> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            var exists = await _comicValidator.ExistsAsync(request.Dto.ComicId);
            if (!exists)
            {
                return null; // Signals that comic wasn't found
            }

            var comment = new Comment
            {
                UserId = request.UserId,
                ComicId = request.Dto.ComicId,
                Content = request.Dto.Content,
                ParentCommentId = request.Dto.ParentCommentId
            };

            await _repository.AddAsync(comment);

            // Publish message via MassTransit Outbox BEFORE SaveChangesAsync
            await _missionProgressNotifier.RecordAsync(
                request.NumericUserId, MissionType.LeaveComment, comment.Id, comment.CreatedAt);

            await _repository.SaveChangesAsync();

            var result = new CommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                ComicId = comment.ComicId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt
            };

            return result;
        }
    }
}
