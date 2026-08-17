using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using ComicAPI.Application.Interfaces;

namespace ComicAPI.API.Consumers
{
    public class ComicViewedEventConsumer : IConsumer<ComicViewedIntegrationEvent>
    {
        private readonly IComicService _comicService;
        private readonly ILogger<ComicViewedEventConsumer> _logger;

        public ComicViewedEventConsumer(IComicService comicService, ILogger<ComicViewedEventConsumer> logger)
        {
            _comicService = comicService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ComicViewedIntegrationEvent> context)
        {
            _logger.LogInformation("Consuming ComicViewedIntegrationEvent for ComicId: {ComicId}", context.Message.ComicId);
            
            var success = await _comicService.IncrementViewCountAsync(context.Message.ComicId);
            if (!success)
            {
                _logger.LogWarning("Failed to increment view for ComicId: {ComicId}", context.Message.ComicId);
            }
        }
    }
}
