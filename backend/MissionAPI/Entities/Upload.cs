using System;
using SharedKernel.Entities;

namespace MissionAPI.Entities
{
    public class Upload : BaseEntity
    {
        public int UserId { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string PublicId { get; set; } = string.Empty;
    }
}
