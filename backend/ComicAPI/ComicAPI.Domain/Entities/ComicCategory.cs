using System;
using SharedKernel.Entities;

namespace ComicAPI.Domain.Entities
{
    public class ComicCategory : BaseEntity
    {
        public Guid ComicId { get; set; }
        public Guid CategoryId { get; set; }

        public virtual Comic Comic { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
    }
}
