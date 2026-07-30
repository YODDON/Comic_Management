using System;
using SharedKernel.Entities;

namespace MissionAPI.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
