using System;
using System.Linq;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Entities;

namespace UserAPI.Application.Features.Admin
{
    public static class AdminHelper
    {
        public static readonly string[] AssignableRoles = ["Reader", "Guest"];

        public static bool IsAdmin(User user) => user.UserRoles.Any(item =>
            item.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        public static void RevokeRefreshTokens(User user)
        {
            foreach (var token in user.RefreshTokens.Where(token => !token.IsRevoked))
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
            }
        }

        public static AdminUserDto ToDto(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            IsEmailVerified = user.IsEmailVerified,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = user.UserRoles.Select(item => item.Role.Name).ToList()
        };
    }
}
