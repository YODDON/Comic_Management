using System.Security.Claims;
using UserAPI.Domain.Entities;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Services;

public class UserQueryService : IUserQueryService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserQueryService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public Task<User?> GetUserByIdAsync(int id) => _userRepository.GetUserByIdAsync(id);
    public Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> ids) => _userRepository.GetUsersByIdsAsync(ids);
    public ClaimsPrincipal? ValidateToken(string token) => _jwtTokenGenerator.ValidateToken(token);
}
