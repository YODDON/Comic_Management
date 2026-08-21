namespace PaymentAPI.DTOs;

public class ChapterPurchaseResultDto
{
    public Guid ChapterId { get; set; }
    public decimal Price { get; set; }
    public decimal? RemainingBalance { get; set; }
    public bool AlreadyPurchased { get; set; }
}
