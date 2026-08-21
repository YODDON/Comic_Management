using System;
using MassTransit;
using PaymentAPI.Domain.Entities;
using SharedKernel.Contracts.Purchase;

namespace PaymentAPI.Application.Sagas
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        public State ProcessingPayment { get; private set; }
        public State UnlockingChapter { get; private set; }
        public State RefundingWallet { get; private set; }
        public State Failed { get; private set; }
        
        public Event<SubmitPurchaseCommand> SubmitPurchaseEvent { get; private set; }
        public Event<WalletDebitedEvent> WalletDebitedEvent { get; private set; }
        public Event<WalletDebitFailedEvent> WalletDebitFailedEvent { get; private set; }
        public Event<ChapterUnlockedEvent> ChapterUnlockedEvent { get; private set; }
        public Event<ChapterUnlockFailedEvent> ChapterUnlockFailedEvent { get; private set; }
        public Event<WalletRefundedEvent> WalletRefundedEvent { get; private set; }

        public PurchaseStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => SubmitPurchaseEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WalletDebitedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WalletDebitFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ChapterUnlockedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ChapterUnlockFailedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WalletRefundedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(SubmitPurchaseEvent)
                    .Then(context =>
                    {
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.UserId = context.Message.UserId;
                        context.Saga.ComicId = context.Message.ComicId;
                        context.Saga.ChapterId = context.Message.ChapterId;
                        context.Saga.Price = context.Message.Price;
                        context.Saga.TransactionId = context.Message.TransactionId;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .SendAsync(new Uri("queue:debit-wallet"), context => context.Init<DebitWalletCommand>(new
                    {
                        context.Message.CorrelationId,
                        context.Message.UserId,
                        Amount = context.Message.Price,
                        context.Message.TransactionId
                    }))
                    .TransitionTo(ProcessingPayment)
            );

            During(ProcessingPayment,
                When(WalletDebitedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .SendAsync(new Uri("queue:unlock-chapter"), context => context.Init<UnlockChapterCommand>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.UserId,
                        context.Saga.ChapterId
                    }))
                    .TransitionTo(UnlockingChapter),
                
                When(WalletDebitFailedEvent)
                    .Then(context => 
                    {
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                        context.Saga.ErrorReason = context.Message.Reason;
                    })
                    .RespondAsync(context => context.Init<PurchaseFailedEvent>(new 
                    {
                        context.Saga.CorrelationId,
                        Reason = context.Message.Reason,
                        context.Saga.TransactionId
                    }))
                    .TransitionTo(Failed)
            );

            During(UnlockingChapter,
                When(ChapterUnlockedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .RespondAsync(context => context.Init<PurchaseCompletedEvent>(new 
                    {
                        context.Saga.CorrelationId,
                        RemainingBalance = -1m,
                        context.Saga.TransactionId,
                        context.Saga.ComicId
                    }))
                    .Finalize(),
                
                When(ChapterUnlockFailedEvent)
                    .Then(context =>
                    {
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                        context.Saga.ErrorReason = context.Message.Reason;
                    })
                    .SendAsync(new Uri("queue:refund-wallet"), context => context.Init<RefundWalletCommand>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.UserId,
                        Amount = context.Saga.Price,
                        context.Saga.TransactionId
                    }))
                    .TransitionTo(RefundingWallet)
            );

            During(RefundingWallet,
                When(WalletRefundedEvent)
                    .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                    .RespondAsync(context => context.Init<PurchaseFailedEvent>(new 
                    {
                        context.Saga.CorrelationId,
                        Reason = context.Saga.ErrorReason ?? "Saga failed and refunded",
                        context.Saga.TransactionId
                    }))
                    .TransitionTo(Failed)
            );

            SetCompletedWhenFinalized();
        }
    }
}
