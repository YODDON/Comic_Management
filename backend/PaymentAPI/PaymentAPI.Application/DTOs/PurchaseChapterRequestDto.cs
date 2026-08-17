using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentAPI.DTOs
{
    public class PurchaseChapterRequestDto
    {
        [Required]
        public Guid ChapterId { get; set; }
    }
}
