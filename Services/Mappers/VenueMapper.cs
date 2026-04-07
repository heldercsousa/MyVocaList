using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

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
            EventCount = eventCount
        };
    }
}
