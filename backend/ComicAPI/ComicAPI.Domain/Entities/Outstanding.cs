using System;
using SharedKernel.Entities;

namespace ComicAPI.Domain.Entities
{
    public class Outstanding : BaseEntity
    {
        public Guid ComicId { get; set; }
        public int Priority { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual Comic Comic { get; set; } = null!;
    }
}
