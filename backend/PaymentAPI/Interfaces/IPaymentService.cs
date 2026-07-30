using System;
using System.Threading.Tasks;
using SharedKernel.Responses;

namespace PaymentAPI.Interfaces
{
    public interface IPaymentService
    {
        Task<decimal> GetBalanceAsync(int userId);
        Task<ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>> PurchaseChapterAsync(int userId, Guid chapterId);
        Task<ApiResponse<PagedResult<PaymentAPI.DTOs.TransactionDto>>> GetTransactionsAsync(
            int? userId,
            string? type,
            string? status,
            string? search,
            int pageNumber,
            int pageSize);
        Task<ApiResponse<PaymentAPI.DTOs.TransactionDto>> GetTransactionByIdAsync(Guid id);
        Task<ApiResponse<PaymentAPI.DTOs.DepositResponseDto>> CreateDepositAsync(int userId, decimal amount);
        Task<ApiResponse<bool>> ProcessSePayWebhookAsync(PaymentAPI.DTOs.SePayWebhookDto request);
    }
}
