using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserAPI.Domain.Entities;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.UserQueries.Queries
{
    public class GetUserByIdQuery : IRequest<User?>
    {
        public int Id { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User?>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return _userRepository.GetUserByIdAsync(request.Id);
        }
    }
}
