namespace MyVocaList.Domain.Entity;

public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; }

    /// <summary>Absolute https picture URL or a well-formed local reference, or null (REQ-VEN-01). Bytes are never stored.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Street address, first line (REQ-VEN-12).</summary>
    public string? AddressLine1 { get; set; }
    /// <summary>Address line 2, e.g. a suite or a building block (REQ-VEN-12).</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>City (REQ-VEN-12).</summary>
    public string? City { get; set; }
    /// <summary>State, province or region (REQ-VEN-12).</summary>
    public string? Region { get; set; }
    /// <summary>Postal code (REQ-VEN-12).</summary>
    public string? PostalCode { get; set; }
    /// <summary>Country (REQ-VEN-12).</summary>
    public string? Country { get; set; }
    /// <summary>Latitude of the pinned place, or null; set together with <see cref="Longitude"/>.</summary>
    public double? Latitude { get; set; }
    /// <summary>Longitude of the pinned place, or null; set together with <see cref="Latitude"/>.</summary>
    public double? Longitude { get; set; }

    public ICollection<Event> Events { get; set; } // Navigation property
}
