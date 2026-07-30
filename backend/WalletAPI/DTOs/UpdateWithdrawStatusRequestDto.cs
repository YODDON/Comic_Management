using System.ComponentModel.DataAnnotations;
using SharedKernel.Enums;

namespace WalletAPI.DTOs;

public class UpdateWithdrawStatusRequestDto
{
    public WithdrawStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}
