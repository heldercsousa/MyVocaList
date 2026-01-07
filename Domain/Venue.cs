namespace MyVocaList.Domain;

public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; }

    public ICollection<Event> Events { get; set; } // Navigation property
}
