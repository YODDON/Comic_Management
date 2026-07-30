using System;
using System.Collections.Generic;

namespace UserAPI.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
