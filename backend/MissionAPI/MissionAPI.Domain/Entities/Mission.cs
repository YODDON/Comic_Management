using System;
using System.Collections.Generic;
using SharedKernel.Entities;
using SharedKernel.Enums;

namespace MissionAPI.Entities
{
    public class Mission : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardCoin { get; set; }
        public MissionType Type { get; set; }
        public int TargetCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public virtual ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
    }
}
