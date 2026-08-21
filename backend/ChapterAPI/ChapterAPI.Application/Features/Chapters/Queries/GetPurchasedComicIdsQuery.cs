using AutoMapper;
using MassTransit;
using SharedKernel.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Features.Chapters.Queries
{
    public class GetPurchasedComicIdsQuery : IRequest<List<Guid>>
    {
        public int UserId { get; set; }
    }

    public class GetPurchasedComicIdsQueryHandler : IRequestHandler<GetPurchasedComicIdsQuery, List<Guid>>
    {
        private readonly IChapterRepository _repository;

        public GetPurchasedComicIdsQueryHandler(IChapterRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Guid>> Handle(GetPurchasedComicIdsQuery request, CancellationToken cancellationToken)
        {
            return _repository.GetPurchasedComicIdsAsync(request.UserId);
        }
    }
}
