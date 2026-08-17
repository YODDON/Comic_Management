using AutoMapper;
using MassTransit;
using SharedKernel.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ChapterAPI.Entities;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Features.Chapters.Queries
{
    public class GetChapterInfoQuery : IRequest<Chapter?>
    {
        public Guid ChapterId { get; set; }
    }

    public class GetChapterInfoQueryHandler : IRequestHandler<GetChapterInfoQuery, Chapter?>
    {
        private readonly IChapterRepository _repository;

        public GetChapterInfoQueryHandler(IChapterRepository repository)
        {
            _repository = repository;
        }

        public Task<Chapter?> Handle(GetChapterInfoQuery request, CancellationToken cancellationToken)
        {
            return _repository.GetChapterByIdAsync(request.ChapterId, false);
        }
    }
}
