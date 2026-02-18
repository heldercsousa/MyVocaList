namespace MyVocaList.Domain.Entity;

public class EventParticipation
{
    public int Id { get; set; }
    public int PersonId { get; set; } // Foreign key
    public int EventId { get; set; } // Foreign key
    public DateTime Timestamp { get; set; } // When the action occurred
    public ParticipationStatus Status { get; set; } // Participation status

    public Person Person { get; set; } // Navigation property
    public Event Event { get; set; } // Navigation property
}

public enum ParticipationStatus
{
    Absent = 0,
    Present = 1
}
