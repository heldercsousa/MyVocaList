namespace MyVocaList.Domain.Entity;

public class Event
{
    public int Id { get; set; }
    public int VenueId { get; set; } // Foreign key
    public DateTime EventDate { get; set; }
    public string EventName { get; set; }
    public bool QueueActive { get; set; }

    public Venue Venue { get; set; } // Navigation property
    public ICollection<EventParticipation> Participations { get; set; } // Navigation property
}
