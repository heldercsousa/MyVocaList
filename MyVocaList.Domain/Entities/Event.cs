namespace MyVocaList.Domain.Entities;

using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Interfaces;

public sealed class Event : IAggregateRoot
{
    public int Id { get; set; }

    public int VenueId { get; set; }

    public required string Name { get; set; }

    public DateTime? ScheduledStartTime { get; set; }

    public DateTime? ScheduledEndTime { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public EventStatus Status { get; set; }

    public string Mode { get; set; } = "VideoKaraoke";

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    // Navigation properties
    public required virtual Venue Venue { get; set; }

    public virtual ICollection<QueueEntry> QueueEntries { get; set; } = [];
}
