using System;

namespace SharedKernel.Events
{
    public class ComicViewedIntegrationEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid ComicId { get; set; }
        
        public ComicViewedIntegrationEvent(Guid comicId)
        {
            ComicId = comicId;
        }

        public ComicViewedIntegrationEvent() { }
    }
}
