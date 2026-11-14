using MyVocaList.Contracts.DTOs.Venues;

namespace MyVocaList.UI.Messages;

/// <summary>Sent by the map picker when the user confirms a place; the venue form receives it (REQ-VEN-30).</summary>
public sealed record VenuePlacePickedMessage(VenuePlace Place);
