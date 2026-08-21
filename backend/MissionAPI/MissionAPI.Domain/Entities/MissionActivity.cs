using SharedKernel.Entities;

namespace MissionAPI.Entities;

public class MissionActivity : BaseEntity
{
    public int UserId { get; set; }
    public Guid MissionId { get; set; }
    public Guid ActivityId { get; set; }
    public DateTime OccurredAt { get; set; }

    public Mission Mission { get; set; } = null!;
}
