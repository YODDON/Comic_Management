using AutoMapper;
using MassTransit;
using SharedKernel.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Features.Chapters.Queries
{
    public class IsChapterPurchasedQuery : IRequest<bool>
    {
        public int UserId { get; set; }
        public Guid ChapterId { get; set; }
    }

    public class IsChapterPurchasedQueryHandler : IRequestHandler<IsChapterPurchasedQuery, bool>
    {
        private readonly IChapterRepository _repository;

        public IsChapterPurchasedQueryHandler(IChapterRepository repository)
        {
            _repository = repository;
        }

        public Task<bool> Handle(IsChapterPurchasedQuery request, CancellationToken cancellationToken)
        {
            return _repository.HasUserPurchasedChapterAsync(request.UserId, request.ChapterId);
        }
    }
}
