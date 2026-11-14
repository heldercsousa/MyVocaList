namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// Open-data references a looked-up place names for its picture (REQ-VEN-37). Every value is optional;
/// all three null means the place names no picture.
/// </summary>
/// <param name="WikidataId">Wikidata entity id (<c>Q</c> followed by digits), or null.</param>
/// <param name="CommonsFile">Wikimedia Commons file name without the <c>File:</c> prefix, or null.</param>
/// <param name="ImageUrl">Absolute https image address, or null.</param>
public sealed record VenuePictureSource(string? WikidataId, string? CommonsFile, string? ImageUrl);
