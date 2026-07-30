namespace BannerAPI.DTOs;

public class BannerDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
