using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Services.Mappers;

/// <summary>
/// Maps Venue domain entities to DTOs
/// </summary>
public static class VenueMapper
{
    /// <summary>
    /// Converts Venue entity to list item DTO
    /// </summary>
    public static VenueListItemDto ToListDto(Venue venue, int eventCount)
    {
        return new VenueListItemDto
        {
            Id = venue.Id,
            Name = venue.Name,
            EventCount = eventCount,
            ImageUrl = venue.ImageUrl,
            AddressSummary = VenueAddressRules.Summary(new VenueAddress(
                venue.AddressLine1, venue.AddressLine2, venue.City, venue.Region, venue.PostalCode, venue.Country))
        };
    }
}
