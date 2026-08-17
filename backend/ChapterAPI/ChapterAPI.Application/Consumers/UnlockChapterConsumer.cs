using System;
using System.Threading.Tasks;
using MassTransit;
using SharedKernel.Contracts.Purchase;
using ChapterAPI.Interfaces;

namespace ChapterAPI.Application.Consumers
{
    public class UnlockChapterConsumer : IConsumer<UnlockChapterCommand>
    {
        private readonly IChapterRepository _repository;

        public UnlockChapterConsumer(IChapterRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<UnlockChapterCommand> context)
        {
            var msg = context.Message;
            
            try
            {
                var unlocked = await _repository.UnlockChapterAsync(msg.UserId, msg.ChapterId);
                // `unlocked` is true if a new record was added, or false if already unlocked.
                // In both cases, the chapter is considered unlocked.
                await context.Publish(new ChapterUnlockedEvent(msg.CorrelationId));
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await context.Publish(new ChapterUnlockFailedEvent(msg.CorrelationId, $"Lỗi hệ thống khi mở khóa chapter: {ex.Message}"));
            }
        }
    }
}
