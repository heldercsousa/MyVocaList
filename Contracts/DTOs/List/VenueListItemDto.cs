namespace MyVocaList.Contracts.DTOs.List
{
    public class VenueListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int EventCount { get; set; }
        public bool HasEvents => EventCount > 0;
    }
}
