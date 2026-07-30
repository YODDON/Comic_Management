using System;
using SharedKernel.Entities;

namespace SocialAPI.Entities
{
    public class Follow : BaseEntity
    {
        public Guid FollowerId { get; set; }
        public Guid FollowingId { get; set; }
    }
}
