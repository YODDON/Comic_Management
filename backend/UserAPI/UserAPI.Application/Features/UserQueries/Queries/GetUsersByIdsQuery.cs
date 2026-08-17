using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserAPI.Domain.Entities;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.UserQueries.Queries
{
    public class GetUsersByIdsQuery : IRequest<List<User>>
    {
        public IEnumerable<int> Ids { get; set; } = new List<int>();
    }

    public class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, List<User>>
    {
        private readonly IUserRepository _userRepository;

        public GetUsersByIdsQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<List<User>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
        {
            return _userRepository.GetUsersByIdsAsync(request.Ids);
        }
    }
}
