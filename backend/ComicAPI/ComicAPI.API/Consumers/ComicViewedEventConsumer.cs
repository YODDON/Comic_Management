using System.Threading.Tasks;
using MassTransit;
using SharedKernel.Events;
using MediatR;
using ComicAPI.Application.Features.Comics.Commands;

namespace ComicAPI.API.Consumers
{
    public class ComicViewedEventConsumer : IConsumer<ComicViewedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public ComicViewedEventConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Consume(ConsumeContext<ComicViewedIntegrationEvent> context)
        {
            var message = context.Message;
            await _mediator.Send(new IncrementComicViewCommand { ComicId = message.ComicId });
        }
    }
}
