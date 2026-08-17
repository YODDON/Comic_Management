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
    public class DeleteCommentCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly ICommentRepository _repository;
        private readonly IComicValidator _comicValidator;
        private readonly IMissionProgressNotifier _missionProgressNotifier;

        public DeleteCommentCommandHandler(ICommentRepository repository, IComicValidator comicValidator, IMissionProgressNotifier missionProgressNotifier)
        {
            _repository = repository;
            _comicValidator = comicValidator;
            _missionProgressNotifier = missionProgressNotifier;
        }

        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _repository.GetByIdAsync(request.Id);
            if (comment == null)
            {
                return false;
            }

            if (comment.UserId != request.UserId && !request.IsAdmin)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this comment.");
            }

            _repository.Remove(comment);

            var replies = await _repository.GetRepliesByParentCommentIdAsync(request.Id);
            _repository.RemoveRange(replies);

            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
