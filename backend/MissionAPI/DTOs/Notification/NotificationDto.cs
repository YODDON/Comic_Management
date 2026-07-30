using System;

namespace MissionAPI.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        // If null or empty, it will be considered as a broadcast notification (Guid.Empty)
        public int? UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class UpdateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
