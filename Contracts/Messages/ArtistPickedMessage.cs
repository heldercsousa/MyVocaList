using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Contracts.Messages;

public sealed record ArtistPickedMessage(MusicSearchResultDto Result);
