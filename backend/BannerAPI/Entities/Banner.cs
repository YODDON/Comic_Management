using SharedKernel.Entities;

namespace BannerAPI.Entities
{
    public class Banner : BaseEntity
    {
        public string? Title { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImagePublicId { get; set; }
        public string? TargetUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
