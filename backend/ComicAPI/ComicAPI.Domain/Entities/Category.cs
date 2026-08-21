using System;
using System.Collections.Generic;
using SharedKernel.Entities;

namespace ComicAPI.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;

        public virtual ICollection<ComicCategory> ComicCategories { get; set; } = new List<ComicCategory>();
    }
}
