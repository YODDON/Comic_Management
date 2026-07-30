using System;

namespace UserAPI.Entities
{
    public class VerificationCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // e.g. "EmailVerification", "ResetPassword"
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
