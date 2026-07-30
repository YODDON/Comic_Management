using System;
using SharedKernel.Entities;

namespace SocialAPI.Entities
{
    public class Favorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ComicId { get; set; }
    }
}
