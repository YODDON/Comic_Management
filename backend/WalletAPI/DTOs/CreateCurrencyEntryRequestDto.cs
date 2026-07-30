using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace WalletAPI.DTOs;

public class CreateCurrencyEntryRequestDto
{
    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
