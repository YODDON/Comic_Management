using Grpc.Core;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UserAPI.Interfaces;
using UserAPI.Protos;

namespace UserAPI.GrpcServices
{
    public class UserGrpcService : UserService.UserServiceBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public UserGrpcService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public override async Task<UserResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
        {
            if (!int.TryParse(request.Id, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
            }

            var response = new UserResponse
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                IsActive = user.IsActive
            };

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            response.Roles.AddRange(roles);

            return response;
        }

        public override async Task<UsersListResponse> GetUsersByIds(GetUsersByIdsRequest request, ServerCallContext context)
        {
            var userIds = new System.Collections.Generic.List<int>();
            foreach (var idStr in request.Ids)
            {
                if (int.TryParse(idStr, out var id))
                {
                    userIds.Add(id);
                }
            }

            var users = await _userRepository.GetUsersByIdsAsync(userIds);
            var response = new UsersListResponse();

            foreach (var user in users)
            {
                var usrResponse = new UserResponse
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl ?? string.Empty,
                    IsActive = user.IsActive
                };
                
                var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
                usrResponse.Roles.AddRange(roles);
                
                response.Users.Add(usrResponse);
            }

            return response;
        }

        public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Task.FromResult(new ValidateTokenResponse { IsValid = false });
            }

            var claimsPrincipal = _jwtTokenGenerator.ValidateToken(request.Token);
            if (claimsPrincipal == null)
            {
                return Task.FromResult(new ValidateTokenResponse { IsValid = false });
            }

            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaims = claimsPrincipal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var response = new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userIdClaim ?? string.Empty
            };
            response.Roles.AddRange(roleClaims);

            return Task.FromResult(response);
        }
    }
}
