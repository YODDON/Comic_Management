using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SocialAPI.DTOs;
using SocialAPI.Interfaces;

namespace SocialAPI.Application.Features.SocialActivities.Queries
{
    public class GetUserActivitiesQuery : IRequest<SocialActivitySnapshotDto>
    {
        public int NumericUserId { get; set; }
    }

    public class GetUserActivitiesQueryHandler : IRequestHandler<GetUserActivitiesQuery, SocialActivitySnapshotDto>
    {
        private readonly ISocialActivityRepository _repository;

        public GetUserActivitiesQueryHandler(ISocialActivityRepository repository)
        {
            _repository = repository;
        }

        public Task<SocialActivitySnapshotDto> Handle(GetUserActivitiesQuery request, CancellationToken cancellationToken)
        {
            var userId = new Guid(
                System.Security.Cryptography.MD5.HashData(BitConverter.GetBytes(request.NumericUserId)));
            return _repository.GetUserActivitiesAsync(userId, cancellationToken);
        }
    }
}
