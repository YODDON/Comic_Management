using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using SharedKernel.Enums;

namespace WalletAPI.Services;

public class WithdrawService : IWithdrawService
{
    private readonly IWithdrawRepository _repository;

    public WithdrawService(IWithdrawRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<WithdrawableDto>> GetWithdrawableAsync(int userId)
    {
        var (balance, withdrawable) = await _repository.GetWithdrawableAsync(userId);
        return new ApiResponse<WithdrawableDto>(new WithdrawableDto
        {
            Balance = balance,
            Withdrawable = withdrawable,
            Locked = balance - withdrawable
        });
    }

    public async Task<ApiResponse<WithdrawDto>> CreateAsync(int userId, CreateWithdrawRequestDto request)
    {
        if (request.Amount <= 0)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Amount must be greater than zero.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.BankAccount) || string.IsNullOrWhiteSpace(request.BankName))
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Bank account and bank name are required.", 400);
        }

        var (withdraw, balance, pendingExists, insufficientBalance) = await _repository.CreateAsync(
            userId,
            request.Amount,
            request.BankAccount,
            request.BankName,
            request.AccountName);

        if (pendingExists)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse(
                "A pending withdrawal request already exists.",
                409);
        }

        if (insufficientBalance)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Insufficient wallet balance.", 422);
        }

        var result = new WithdrawDto
        {
            Id = withdraw!.Id,
            UserId = userId,
            Amount = withdraw.Amount,
            Fee = WalletRules.WithdrawFee(withdraw.Amount),
            TotalDeducted = withdraw.Amount + WalletRules.WithdrawFee(withdraw.Amount),
            BankAccount = withdraw.BankAccount,
            BankName = withdraw.BankName,
            AccountName = withdraw.AccountName,
            Status = withdraw.Status,
            Note = withdraw.Note,
            CreatedAt = withdraw.CreatedAt,
            ProcessedAt = withdraw.ProcessedAt,
            Balance = balance
        };

        return new ApiResponse<WithdrawDto>(result, "Withdrawal request created successfully.", 201);
    }

    public async Task<ApiResponse<PagedResult<WithdrawDto>>> GetMineAsync(
        int userId,
        int pageNumber,
        int pageSize)
    {
        var validationError = ValidatePaging(pageNumber, pageSize);
        if (validationError is not null)
        {
            return validationError;
        }

        var (items, totalCount) = await _repository.GetMineAsync(userId, pageNumber, pageSize);
        return CreatePagedResponse(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ApiResponse<PagedResult<WithdrawDto>>> GetAdminAsync(
        WithdrawStatus? status,
        string? search,
        int pageNumber,
        int pageSize)
    {
        var validationError = ValidatePaging(pageNumber, pageSize);
        if (validationError is not null)
        {
            return validationError;
        }

        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            return ApiResponse<PagedResult<WithdrawDto>>.ErrorResponse("Withdraw status is invalid.", 400);
        }

        var (items, totalCount) = await _repository.GetAdminAsync(status, search, pageNumber, pageSize);
        return CreatePagedResponse(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ApiResponse<WithdrawDto>> UpdateStatusAsync(
        Guid id,
        UpdateWithdrawStatusRequestDto request)
    {
        if (id == Guid.Empty)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Withdraw id is invalid.", 400);
        }

        if (request.Status is not (WithdrawStatus.Approved or WithdrawStatus.Rejected))
        {
            return ApiResponse<WithdrawDto>.ErrorResponse(
                "Status must be Approved or Rejected.",
                400);
        }

        var (withdraw, balance, notFound, alreadyProcessed) = await _repository.UpdateStatusAsync(
            id,
            request.Status,
            request.Note);

        if (notFound)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Withdrawal request not found.", 404);
        }

        if (alreadyProcessed)
        {
            return ApiResponse<WithdrawDto>.ErrorResponse("Withdrawal request has already been processed.", 409);
        }

        var result = new WithdrawDto
        {
            Id = withdraw!.Id,
            UserId = withdraw.Wallet.UserId,
            Amount = withdraw.Amount,
            Fee = WalletRules.WithdrawFee(withdraw.Amount),
            TotalDeducted = withdraw.Amount + WalletRules.WithdrawFee(withdraw.Amount),
            BankAccount = withdraw.BankAccount,
            BankName = withdraw.BankName,
            AccountName = withdraw.AccountName,
            Status = withdraw.Status,
            Note = withdraw.Note,
            CreatedAt = withdraw.CreatedAt,
            ProcessedAt = withdraw.ProcessedAt,
            Balance = balance
        };

        var message = request.Status == WithdrawStatus.Approved
            ? "Withdrawal approved. External transfer recorded."
            : "Withdrawal rejected and balance refunded.";
        return new ApiResponse<WithdrawDto>(result, message);
    }

    private static ApiResponse<PagedResult<WithdrawDto>>? ValidatePaging(int pageNumber, int pageSize)
    {
        return pageNumber < 1 || pageSize is < 1 or > 100
            ? ApiResponse<PagedResult<WithdrawDto>>.ErrorResponse(
                "Page number must be at least 1 and page size must be between 1 and 100.",
                400)
            : null;
    }

    private static ApiResponse<PagedResult<WithdrawDto>> CreatePagedResponse(
        List<Entities.Withdraw> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var dtos = items.Select(x => new WithdrawDto
        {
            Id = x.Id,
            UserId = x.Wallet.UserId,
            Amount = x.Amount,
            Fee = WalletRules.WithdrawFee(x.Amount),
            TotalDeducted = x.Amount + WalletRules.WithdrawFee(x.Amount),
            BankAccount = x.BankAccount,
            BankName = x.BankName,
            AccountName = x.AccountName,
            Status = x.Status,
            Note = x.Note,
            CreatedAt = x.CreatedAt,
            ProcessedAt = x.ProcessedAt
        }).ToList();

        var result = new PagedResult<WithdrawDto>(dtos, totalCount, pageNumber, pageSize);
        return new ApiResponse<PagedResult<WithdrawDto>>(result, "Withdrawal requests retrieved successfully.");
    }
}
