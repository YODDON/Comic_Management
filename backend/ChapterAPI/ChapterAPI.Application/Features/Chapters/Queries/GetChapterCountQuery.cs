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
    public class GetChapterCountQuery : IRequest<int>
    {
        public Guid ComicId { get; set; }
    }

    public class GetChapterCountQueryHandler : IRequestHandler<GetChapterCountQuery, int>
    {
        private readonly IChapterRepository _repository;

        public GetChapterCountQueryHandler(IChapterRepository repository)
        {
            _repository = repository;
        }

        public Task<int> Handle(GetChapterCountQuery request, CancellationToken cancellationToken)
        {
            return _repository.GetChapterCountAsync(request.ComicId);
        }
    }
}
