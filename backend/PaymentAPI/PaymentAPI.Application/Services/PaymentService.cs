using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ChapterAPI.Protos;
using PaymentAPI.Entities;
using PaymentAPI.Interfaces;
using SharedKernel.Enums;
using SharedKernel.Responses;
using Grpc.Net.Client;
using Grpc.Core;
using WalletAPI.Protos;
using Microsoft.Extensions.Options;
using PaymentAPI.Settings;
using MassTransit;
using SharedKernel.Contracts.Purchase;

namespace PaymentAPI.Services
{
    public class PaymentService : IPaymentService
    {
        /// <summary>VietQR's official image endpoint. Free, no account, no signature.</summary>
        private const string VietQrImageBaseUrl = "https://img.vietqr.io/image";

        private readonly IPaymentRepository _repository;
        private readonly ChapterGrpc.ChapterGrpcClient _chapterGrpcClient;
        private readonly WalletService.WalletServiceClient _walletGrpcClient;
        private readonly IRequestClient<SubmitPurchaseCommand> _purchaseClient;
        private readonly TopUpSettings _topUpSettings;
        private readonly BankSettings _bankSettings;

        public PaymentService(
            IPaymentRepository repository,
            ChapterGrpc.ChapterGrpcClient chapterGrpcClient,
            WalletService.WalletServiceClient walletGrpcClient,
            IRequestClient<SubmitPurchaseCommand> purchaseClient,
            IOptions<TopUpSettings> topUpOptions,
            IOptions<BankSettings> bankOptions)
        {
            _repository = repository;
            _chapterGrpcClient = chapterGrpcClient;
            _walletGrpcClient = walletGrpcClient;
            _purchaseClient = purchaseClient;
            _topUpSettings = topUpOptions.Value;
            _bankSettings = bankOptions.Value;
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var transactions = await _repository.GetUserTransactionsAsync(userId);
            
            decimal balance = 0;
            foreach (var tx in transactions)
            {
                if (tx.Type == TransactionType.ManualTopUp || tx.Type == TransactionType.Refund)
                {
                    balance += tx.Amount;
                }
                else if (tx.Type == TransactionType.Purchase || tx.Type == TransactionType.Withdraw)
                {
                    balance -= tx.Amount;
                }
            }

            return balance;
        }

        public async Task<ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>> PurchaseChapterAsync(
            int userId,
            Guid chapterId)
        {
            var existingPurchase = await _repository.GetUserPurchaseAsync(userId, chapterId);
            if (existingPurchase != null)
            {
                // Repair a partial cross-service purchase if PaymentDB committed previously but the
                // corresponding ChapterDB unlock did not.
                try
                {
                    var existingUnlock = await _chapterGrpcClient.UnlockChapterAsync(new UnlockChapterRequest
                    {
                        UserId = userId.ToString(),
                        ChapterId = chapterId.ToString()
                    });
                    if (!existingUnlock.Success)
                    {
                        return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                            "Không thể khôi phục quyền đọc chapter đã mua.", 502);
                    }
                }
                catch (RpcException)
                {
                    return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                        "ChapterAPI đang tạm thời không khả dụng.", 503);
                }

                return new ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>(
                    new PaymentAPI.DTOs.ChapterPurchaseResultDto
                    {
                        ChapterId = chapterId,
                        Price = existingPurchase.Price,
                        AlreadyPurchased = true
                    },
                    "Chapter này đã được mở khóa trước đó.");
            }

            GetChapterInfoResponse chapterInfo;
            try
            {
                chapterInfo = await _chapterGrpcClient.GetChapterInfoAsync(
                    new GetChapterInfoRequest { ChapterId = chapterId.ToString() });
            }
            catch (RpcException)
            {
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Không thể lấy thông tin chapter. Vui lòng thử lại sau.", 503);
            }

            if (!chapterInfo.Exists)
            {
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Không tìm thấy chapter.", 404);
            }

            if (!string.Equals(chapterInfo.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Chapter chưa được phát hành.", 404);
            }

            decimal price = (decimal)chapterInfo.UnitPrice;
            if (price <= 0)
            {
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Chapter miễn phí không cần mở khóa.", 400);
            }

            await _repository.BeginTransactionAsync();
            var walletDebited = false;
            decimal? remainingBalance = null;
            var transaction = new Transaction
            {
                UserId = userId,
                ChapterId = chapterId,
                Type = TransactionType.Purchase,
                Amount = price,
                CurrencyType = "Dâu",
                Status = TransactionStatus.Pending
            };

            try
            {
                await _repository.AddTransactionAsync(transaction);
                await _repository.CommitTransactionAsync();

                // Fire the Saga
                var response = await _purchaseClient.GetResponse<PurchaseCompletedEvent, PurchaseFailedEvent>(
                    new SubmitPurchaseCommand(
                        CorrelationId: Guid.NewGuid(),
                        UserId: userId,
                        ComicId: Guid.Parse(chapterInfo.ComicId),
                        ChapterId: chapterId,
                        Price: price,
                        TransactionId: transaction.Id
                    ));

                if (response.Is(out Response<PurchaseCompletedEvent> completed))
                {
                    return new ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>(
                        new PaymentAPI.DTOs.ChapterPurchaseResultDto
                        {
                            ChapterId = chapterId,
                            Price = price,
                            RemainingBalance = completed.Message.RemainingBalance,
                            AlreadyPurchased = false
                        },
                        "Mở khóa chapter thành công.");
                }
                else if (response.Is(out Response<PurchaseFailedEvent> failed))
                {
                    return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                        $"Mở khóa chapter thất bại: {failed.Message.Reason}", 402);
                }
                
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Trạng thái giao dịch không xác định.", 500);
            }
            catch (RequestTimeoutException)
            {
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Giao dịch đang được xử lý ngầm. Vui lòng kiểm tra lại sau.", 202);
            }
            catch (Exception)
            {
                // Unhandled exception means the command might not have been sent.
                transaction.Status = TransactionStatus.Failed;
                transaction.Note = "Lỗi hệ thống khi khởi tạo giao dịch mua.";
                await _repository.SaveFailedTransactionAsync(transaction);
                return ApiResponse<PaymentAPI.DTOs.ChapterPurchaseResultDto>.ErrorResponse(
                    "Đã xảy ra lỗi khi thanh toán. Vui lòng thử lại.", 500);
            }
        }
        public async Task<ApiResponse<PagedResult<PaymentAPI.DTOs.TransactionDto>>> GetTransactionsAsync(
            int? userId,
            string? type,
            string? status,
            string? search,
            int pageNumber,
            int pageSize)
        {
            var (items, totalCount) = await _repository.GetTransactionsAsync(
                userId,
                type,
                status,
                search,
                pageNumber,
                pageSize);

            var dtos = items.Select(t => new PaymentAPI.DTOs.TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type,
                Amount = t.Amount,
                CurrencyType = t.CurrencyType,
                Status = t.Status,
                PaymentMethod = t.PaymentMethod,
                TransactionCode = t.TransactionCode,
                ChapterId = t.ChapterId,
                CreatedAt = t.CreatedAt
            }).ToList();

            await EnrichChapterTransactionsAsync(dtos);

            var paginated = new PagedResult<PaymentAPI.DTOs.TransactionDto>(dtos, totalCount, pageNumber, pageSize);
            return new ApiResponse<PagedResult<PaymentAPI.DTOs.TransactionDto>>(paginated);
        }

        public async Task<ApiResponse<PaymentAPI.DTOs.TransactionDto>> GetTransactionByIdAsync(Guid id)
        {
            var t = await _repository.GetTransactionByIdAsync(id);
            if (t == null)
            {
                return new ApiResponse<PaymentAPI.DTOs.TransactionDto>(null, "Transaction not found", 404);
            }

            var dto = new PaymentAPI.DTOs.TransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type,
                Amount = t.Amount,
                CurrencyType = t.CurrencyType,
                Status = t.Status,
                PaymentMethod = t.PaymentMethod,
                TransactionCode = t.TransactionCode,
                ChapterId = t.ChapterId,
                CreatedAt = t.CreatedAt
            };

            await EnrichChapterTransactionsAsync(new List<PaymentAPI.DTOs.TransactionDto> { dto });

            return new ApiResponse<PaymentAPI.DTOs.TransactionDto>(dto);
        }

        /// <summary>
        /// Creates a pending top-up and returns a VietQR image for the payer to scan.
        /// </summary>
        /// <remarks>
        /// The QR only pre-fills the transfer; the payer's banking app lets them edit the amount, so
        /// the amount that actually arrives may differ from the one requested here. What binds the
        /// transfer to this order is <see cref="Transaction.TransactionCode"/> in the transfer content.
        /// </remarks>
        public async Task<ApiResponse<PaymentAPI.DTOs.DepositResponseDto>> CreateDepositAsync(int userId, decimal amount)
        {
            if (!_bankSettings.IsBankConfigured)
            {
                // 503, not 500: nothing is broken, the server just isn't set up to receive money yet.
                return ApiResponse<PaymentAPI.DTOs.DepositResponseDto>.ErrorResponse(
                    "Deposits are unavailable: the receiving bank account is not configured.", 503);
            }

            if (amount <= 0)
            {
                return ApiResponse<PaymentAPI.DTOs.DepositResponseDto>.ErrorResponse(
                    "Amount must be greater than zero.", 400);
            }

            if (amount != Math.Floor(amount))
            {
                return ApiResponse<PaymentAPI.DTOs.DepositResponseDto>.ErrorResponse(
                    "Amount must be a whole number of VND.", 422);
            }

            var transactionCode = DepositCodeGenerator.Generate();

            var transaction = new Transaction
            {
                UserId = userId,
                Type = SharedKernel.Enums.TransactionType.ManualTopUp,
                Amount = amount,
                CurrencyType = "VND",
                Status = TransactionStatus.Pending,
                PaymentMethod = "VIETQR",
                TransactionCode = transactionCode,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddTransactionAsync(transaction);

            return new ApiResponse<PaymentAPI.DTOs.DepositResponseDto>(new PaymentAPI.DTOs.DepositResponseDto
            {
                TransactionId = transaction.Id,
                TransactionCode = transactionCode,
                Amount = amount,
                QrUrl = BuildVietQrUrl(amount, transactionCode)
            });
        }

        /// <summary>
        /// Builds a VietQR image URL (NAPAS standard, free, no account required).
        /// Format: /image/{bank}-{account}-{template}.png?amount=&amp;addInfo=&amp;accountName=
        /// </summary>
        private string BuildVietQrUrl(decimal amount, string transactionCode)
        {
            // The amount must be a plain integer: "10000", never "10,000.00". Without InvariantCulture
            // a server in a comma-decimal locale would emit an amount the banking app cannot read.
            var amountText = ((long)amount).ToString(CultureInfo.InvariantCulture);

            var url = $"{VietQrImageBaseUrl}/{_bankSettings.BankCode}-{_bankSettings.AccountNumber}-compact2.png"
                    + $"?amount={amountText}"
                    + $"&addInfo={Uri.EscapeDataString(transactionCode)}";

            if (!string.IsNullOrWhiteSpace(_bankSettings.AccountName))
            {
                url += $"&accountName={Uri.EscapeDataString(_bankSettings.AccountName)}";
            }

            return url;
        }

        public async Task<ApiResponse<bool>> ProcessSePayWebhookAsync(PaymentAPI.DTOs.SePayWebhookDto request)
        {
            if (request.TransferType != "in")
            {
                return new ApiResponse<bool>(false, "Not an incoming transfer.");
            }

            if (!DepositCodeGenerator.TryExtract(request.Content, out var transactionCode))
            {
                return new ApiResponse<bool>(false, "Transaction code not found in content.", 404);
            }

            // 1. Check if we already have a transaction tracking THIS specific webhook (by SePay Id)
            var existingTrackingTx = await _repository.GetTransactionByNotePrefixAsync($"SEPAY id={request.Id};");
            
            Transaction targetTransaction;
            decimal requestedAmount;

            if (existingTrackingTx != null)
            {
                if (existingTrackingTx.Status == TransactionStatus.Completed)
                {
                    // Already processed successfully. Idempotent return.
                    return new ApiResponse<bool>(true, "Webhook already processed.");
                }
                
                // It is Pending (failed to credit WalletAPI previously). We retry.
                targetTransaction = existingTrackingTx;
                requestedAmount = targetTransaction.Amount;
            }
            else
            {
                // This webhook has never been seen before.
                var originalTx = await _repository.GetTransactionByCodeAsync(transactionCode);

                if (originalTx == null)
                {
                    return new ApiResponse<bool>(false, "Transaction not found.", 404);
                }

                if (request.TransferAmount <= 0)
                {
                    return new ApiResponse<bool>(false, "Transfer amount must be positive.", 400);
                }

                if (originalTx.Status == TransactionStatus.Pending && string.IsNullOrEmpty(originalTx.Note))
                {
                    // First payment for this QR code. Take over the pending transaction.
                    targetTransaction = originalTx;
                    requestedAmount = targetTransaction.Amount;
                }
                else
                {
                    // The QR code was paid before (or already taken by another webhook).
                    // Create a new transaction for this extra payment.
                    targetTransaction = new Transaction
                    {
                        UserId = originalTx.UserId,
                        Type = SharedKernel.Enums.TransactionType.ManualTopUp,
                        Amount = request.TransferAmount,
                        CurrencyType = "VND",
                        Status = TransactionStatus.Pending,
                        PaymentMethod = "VIETQR",
                        TransactionCode = transactionCode,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repository.AddTransactionAsync(targetTransaction);
                    requestedAmount = request.TransferAmount; // No mismatch for extra payments
                }

                targetTransaction.Amount = request.TransferAmount;
                targetTransaction.Note = BuildSePayNote(request, requestedAmount);
                await _repository.UpdateTransactionAsync(targetTransaction);
            }

            if (!await CreditWalletForTopUpAsync(targetTransaction))
            {
                return ApiResponse<bool>.ErrorResponse("Could not credit the wallet. Transaction stays pending; retry later.", 503);
            }

            targetTransaction.Status = TransactionStatus.Completed;
            await _repository.UpdateTransactionAsync(targetTransaction);

            return new ApiResponse<bool>(true, "Topup transaction completed via SePay Webhook.");
        }

        private static string BuildSePayNote(PaymentAPI.DTOs.SePayWebhookDto request, decimal requestedAmount)
        {
            var note = $"SEPAY id={request.Id}; ref={request.ReferenceCode}; "
                     + $"gateway={request.Gateway}; date={request.TransactionDate}; "
                     + $"received={request.TransferAmount}";

            // Flag the mismatch so an over/underpayment is visible on the transaction record.
            if (requestedAmount != request.TransferAmount)
            {
                note += $"; requested={requestedAmount} (MISMATCH)";
            }

            // Note is nvarchar(1000).
            return note.Length > 1000 ? note[..1000] : note;
        }

        /// <summary>
        /// Credits a confirmed top-up into the user's wallet. Shared by every top-up source
        /// (VietQR admin approval, SePay webhook) so the rules stay in one place.
        /// </summary>
        /// <remarks>
        /// Idempotent by <see cref="Transaction.Id"/>: WalletAPI enforces a unique index on
        /// CurrencyEntry.ReferenceId, so a replayed confirmation credits only once.
        /// Callers must not mark the transaction Completed unless this returns true — otherwise
        /// the transaction looks paid while the balance never moved.
        /// </remarks>
        private async Task<bool> CreditWalletForTopUpAsync(Transaction transaction)
        {
            var coins = transaction.Amount * _topUpSettings.CoinRate;
            if (coins <= 0)
            {
                return false;
            }

            try
            {
                var response = await _walletGrpcClient.AddCoinAsync(new AddCoinRequest
                {
                    UserId = transaction.UserId.ToString(),
                    Amount = (double)coins,
                    ReferenceId = transaction.Id.ToString(),
                    Description = $"Top-up {transaction.TransactionCode}",
                    // Real money in — the only kind of credit that may later be withdrawn as cash.
                    CreditType = SharedKernel.Enums.TransactionType.ManualTopUp.ToString()
                });

                return response.Success;
            }
            catch (RpcException)
            {
                // Wallet unreachable: leave the transaction Pending so it can be retried,
                // rather than completing it against a balance that never changed.
                return false;
            }
        }

        private static Guid CreateRefundReference(Guid transactionId)
        {
            var bytes = transactionId.ToByteArray();
            bytes[15] ^= 0xFF;
            return new Guid(bytes);
        }

        private async Task EnrichChapterTransactionsAsync(
            IEnumerable<PaymentAPI.DTOs.TransactionDto> transactions)
        {
            var purchaseTransactions = transactions
                .Where(transaction => transaction.Type == TransactionType.Purchase
                    && transaction.ChapterId.HasValue)
                .ToList();

            await Task.WhenAll(purchaseTransactions.Select(async transaction =>
            {
                try
                {
                    var chapter = await _chapterGrpcClient.GetChapterInfoAsync(
                        new GetChapterInfoRequest
                        {
                            ChapterId = transaction.ChapterId!.Value.ToString()
                        });
                    if (!chapter.Exists) return;

                    transaction.ComicId = Guid.TryParse(chapter.ComicId, out var comicId)
                        ? comicId
                        : null;
                    transaction.ChapterTitle = chapter.Title;
                    transaction.ChapterNumber = chapter.ChapterNumber;
                    transaction.ChapterSlug = chapter.Slug;
                }
                catch (RpcException)
                {
                    // History remains available with ChapterId even if ChapterAPI is temporarily down.
                }
            }));
        }
    }
}
