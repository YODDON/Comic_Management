using SharedKernel.Responses;
using WalletAPI.DTOs;

namespace WalletAPI.Interfaces;

public interface IWithdrawService
{
    Task<ApiResponse<WithdrawDto>> CreateAsync(int userId, CreateWithdrawRequestDto request);
    Task<ApiResponse<WithdrawableDto>> GetWithdrawableAsync(int userId);
    Task<ApiResponse<PagedResult<WithdrawDto>>> GetMineAsync(int userId, int pageNumber, int pageSize);
    Task<ApiResponse<PagedResult<WithdrawDto>>> GetAdminAsync(
        SharedKernel.Enums.WithdrawStatus? status,
        string? search,
        int pageNumber,
        int pageSize);
    Task<ApiResponse<WithdrawDto>> UpdateStatusAsync(Guid id, UpdateWithdrawStatusRequestDto request);
}
