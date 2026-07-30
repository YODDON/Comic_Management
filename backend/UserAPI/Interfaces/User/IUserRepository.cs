using System.Collections.Generic;
using System.Threading.Tasks;
using UserAPI.Entities;

namespace UserAPI.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> ids);
        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
        Task<User?> GetUserByVerificationCodeAsync(string code, string type);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task AddUserAsync(User user);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task AddVerificationCodeAsync(VerificationCode verificationCode);
        Task SaveChangesAsync();
    }
}
