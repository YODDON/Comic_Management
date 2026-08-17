using AutoMapper;
using MassTransit;
using SharedKernel.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ChapterAPI.Entities;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Features.Chapters.Queries
{
    public class GetUserPurchaseActivitiesQuery : IRequest<List<UserPurchase>>
    {
        public int UserId { get; set; }
    }

    public class GetUserPurchaseActivitiesQueryHandler : IRequestHandler<GetUserPurchaseActivitiesQuery, List<UserPurchase>>
    {
        private readonly IChapterRepository _repository;

        public GetUserPurchaseActivitiesQueryHandler(IChapterRepository repository)
        {
            _repository = repository;
        }

        public Task<List<UserPurchase>> Handle(GetUserPurchaseActivitiesQuery request, CancellationToken cancellationToken)
        {
            return _repository.GetUserPurchaseActivitiesAsync(request.UserId);
        }
    }
}
