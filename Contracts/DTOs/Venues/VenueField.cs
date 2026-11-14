namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// The venue form field a validation failure belongs to; <see cref="None"/> when no single field applies.
/// </summary>
public enum VenueField
{
    /// <summary>No single field.</summary>
    None,

    /// <summary>Venue name.</summary>
    Name,

    /// <summary>Street address.</summary>
    AddressLine1,

    /// <summary>Address line 2.</summary>
    AddressLine2,

    /// <summary>City.</summary>
    City,

    /// <summary>State / Province / Region.</summary>
    Region,

    /// <summary>Postal code.</summary>
    PostalCode,

    /// <summary>Country.</summary>
    Country
}
