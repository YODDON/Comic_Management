using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SharedKernel.Entities;
using SharedKernel.Enums;

namespace ComicAPI.Domain.Entities
{
    public class Comic : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string SalaryType { get; set; } = string.Empty;
        public ComicStatus Status { get; set; } = ComicStatus.Ongoing;
        public int ViewCount { get; set; }

        public virtual ICollection<ComicCategory> ComicCategories { get; set; } = new List<ComicCategory>();
        public virtual ICollection<Outstanding> Outstandings { get; set; } = new List<Outstanding>();
    }
}
