using System.Security.Claims;
using UserAPI.Entities;
using UserAPI.Interfaces;

namespace UserAPI.Services;

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
