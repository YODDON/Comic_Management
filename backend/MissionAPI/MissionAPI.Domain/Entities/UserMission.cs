using System;
using SharedKernel.Entities;

namespace MissionAPI.Entities
{
    public class UserMission : BaseEntity
    {
        public int UserId { get; set; }
        public Guid MissionId { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        public virtual Mission Mission { get; set; } = null!;
    }
}
