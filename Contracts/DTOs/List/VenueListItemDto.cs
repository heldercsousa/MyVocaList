namespace MyVocaList.Contracts.DTOs.List
{
    public class VenueListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int EventCount { get; set; }
        public bool HasEvents => EventCount > 0;

        /// <summary>Venue picture reference (thumbnail shown on the row), or null for the default image (REQ-VEN-06).</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Street address and City joined by ", ", or null when the address is empty (REQ-VEN-18).</summary>
        public string? AddressSummary { get; set; }
    }
}
