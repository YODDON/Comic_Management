using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserAPI.Application.Interfaces;

namespace UserAPI.Application.Features.UserQueries.Queries
{
    public class ValidateTokenQuery : IRequest<ClaimsPrincipal?>
    {
        public string Token { get; set; } = string.Empty;
    }

    public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, ClaimsPrincipal?>
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public ValidateTokenQueryHandler(IJwtTokenGenerator jwtTokenGenerator)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public Task<ClaimsPrincipal?> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_jwtTokenGenerator.ValidateToken(request.Token));
        }
    }
}
