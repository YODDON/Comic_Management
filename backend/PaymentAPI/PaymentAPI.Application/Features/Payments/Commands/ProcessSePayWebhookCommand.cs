using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Grpc.Core;
using MediatR;
using MassTransit;
using SharedKernel.Responses;
using SharedKernel.Enums;
using PaymentAPI.DTOs;
using PaymentAPI.Entities;
using PaymentAPI.Interfaces;
using PaymentAPI.Settings;
using ChapterAPI.Protos;
using WalletAPI.Protos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel.Events;
using SharedKernel.Contracts.Purchase;

using PaymentAPI.Services;
namespace PaymentAPI.Application.Features.Payments.Commands
{
    public class ProcessSePayWebhookCommand : IRequest<ApiResponse<bool>>
    {
        public PaymentAPI.DTOs.SePayWebhookDto Request { get; set; }
    }

    public class ProcessSePayWebhookCommandHandler : IRequestHandler<ProcessSePayWebhookCommand, ApiResponse<bool>>
    {
        private const string VietQrImageBaseUrl = "https://img.vietqr.io/image";
        private readonly IPaymentRepository _repository;
        private readonly ChapterGrpc.ChapterGrpcClient _chapterGrpcClient;
        private readonly WalletService.WalletServiceClient _walletGrpcClient;
        private readonly IRequestClient<SubmitPurchaseCommand> _purchaseClient;
        private readonly TopUpSettings _topUpSettings;
        private readonly BankSettings _bankSettings;

        public ProcessSePayWebhookCommandHandler(IPaymentRepository repository, ChapterGrpc.ChapterGrpcClient chapterGrpcClient, WalletService.WalletServiceClient walletGrpcClient, IRequestClient<SubmitPurchaseCommand> purchaseClient, IOptions<TopUpSettings> topUpOptions, IOptions<BankSettings> bankOptions)
        {
            _repository = repository;
            _chapterGrpcClient = chapterGrpcClient;
            _walletGrpcClient = walletGrpcClient;
            _purchaseClient = purchaseClient;
            _topUpSettings = topUpOptions.Value;
            _bankSettings = bankOptions.Value;
        }

        public async Task<ApiResponse<bool>> Handle(ProcessSePayWebhookCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.TransferType != "in")
            {
                return new ApiResponse<bool>(false, "Not an incoming transfer.");
            }

            if (!DepositCodeGenerator.TryExtract(request.Request.Content, out var transactionCode))
            {
                return new ApiResponse<bool>(false, "Transaction code not found in content.", 404);
            }

            // 1. Check if we already have a transaction tracking THIS specific webhook (by SePay Id)
            var existingTrackingTx = await _repository.GetTransactionByNotePrefixAsync($"SEPAY id={request.Request.Id};");
            
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

                if (request.Request.TransferAmount <= 0)
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
                        Amount = request.Request.TransferAmount,
                        CurrencyType = "VND",
                        Status = TransactionStatus.Pending,
                        PaymentMethod = "VIETQR",
                        TransactionCode = transactionCode,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _repository.AddTransactionAsync(targetTransaction);
                    requestedAmount = request.Request.TransferAmount; // No mismatch for extra payments
                }

                targetTransaction.Amount = request.Request.TransferAmount;
                targetTransaction.Note = BuildSePayNote(request.Request, requestedAmount);
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
