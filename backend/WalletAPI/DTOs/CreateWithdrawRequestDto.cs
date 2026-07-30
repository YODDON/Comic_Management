using System.ComponentModel.DataAnnotations;

namespace WalletAPI.DTOs;

public class CreateWithdrawRequestDto
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(255)]
    public string BankAccount { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AccountName { get; set; } = string.Empty;
}
