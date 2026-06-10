namespace MyVocaList.Domain.Entities;

using MyVocaList.Contracts.Enums;

public sealed class QueueEntry
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int PersonId { get; set; }

    public int? SongId { get; set; }

    public int Position { get; set; }

    public QueueEntryStatus Status { get; set; }

    public DateTime? PerformanceStartTime { get; set; }

    public DateTime? PerformanceEndTime { get; set; }

    public int? PerformanceDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    // Navigation properties
    public required virtual Event Event { get; set; }

    public required virtual Person Person { get; set; }

    public virtual Song? Song { get; set; }
}
