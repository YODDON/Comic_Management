using System;
using SharedKernel.Enums;

namespace MissionAPI.DTOs
{
    public class MissionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardCoin { get; set; }
        public bool IsActive { get; set; }
        public MissionType Type { get; set; }
        public int TargetCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateMissionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardCoin { get; set; }
        public MissionType Type { get; set; }
        public int TargetCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateMissionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardCoin { get; set; }
        public bool IsActive { get; set; }
        public MissionType Type { get; set; }
        public int TargetCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UserMissionDto
    {
        public Guid MissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal RewardCoin { get; set; }
        public MissionType Type { get; set; }
        public int TargetCount { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
