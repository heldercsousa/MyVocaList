using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

/// <summary>REQ-MSRC-12: the cleaning of a YouTube video title before the MusicBrainz query. Pins a sample of real karaoke titles.</summary>
public class KaraokeTitleRulesTests
{
    [Theory]
    // [AC] REQ-MSRC-12: karaoke noise, bracketed or free, is removed from the video title
    [InlineData("Evidências - Chitãozinho & Xororó (Karaokê Version) UPGRADED", null, "Evidências - Chitãozinho & Xororó")]
    [InlineData("Queen - Bohemian Rhapsody (Karaoke Version)", null, "Queen - Bohemian Rhapsody")]
    [InlineData("Let It Be - The Beatles (Karaoke Instrumental Lyrics)", null, "Let It Be - The Beatles")]
    [InlineData("[Karaokê] Garota de Ipanema - Tom Jobim", null, "Garota de Ipanema - Tom Jobim")]
    [InlineData("Karaokê | Evidências - Chitãozinho & Xororó", null, "Evidências - Chitãozinho & Xororó")]
    [InlineData("Imagine - John Lennon | Karaoke | Instrumental | Lyrics", null, "Imagine - John Lennon")]
    [InlineData("Hello - Adele - Karaoke Version", null, "Hello - Adele")]
    [InlineData("Evidências Karaokê - Chitãozinho e Xororó", null, "Evidências - Chitãozinho e Xororó")]
    [InlineData("Shallow - Lady Gaga (Cover) [Karaoke]", null, "Shallow - Lady Gaga")]
    [InlineData("Imagine - John Lennon", null, "Imagine - John Lennon")]
    // [AC] REQ-MSRC-12: en and em dashes are separators; video-release words in brackets are noise too
    [InlineData("Queen – Bohemian Rhapsody (Official Video Remastered)", null, "Queen - Bohemian Rhapsody")]
    [InlineData("Adele — Hello [Official Audio]", null, "Adele - Hello")]
    // [AC] REQ-MSRC-12: the channel name is removed too
    [InlineData("Sing King Karaoke - Shallow - Lady Gaga", "Sing King", "Shallow - Lady Gaga")]
    [InlineData("Shallow - Lady Gaga | Zoom Karaoke", "Zoom Karaoke", "Shallow - Lady Gaga")]
    // a bracket without noise stays
    [InlineData("Faroeste Caboclo (Ao Vivo) (Karaoke)", null, "Faroeste Caboclo (Ao Vivo)")]
    public void Clean_RemovesKaraokeNoiseAndChannelName(string title, string? channel, string expected)
        => Assert.Equal(expected, KaraokeTitleRules.Clean(title, channel));

    [Theory]
    // [AC] REQ-MSRC-12: nothing left after cleaning -> empty text, never null
    [InlineData("Karaoke Instrumental")]
    [InlineData("")]
    [InlineData(null)]
    public void Clean_NothingLeft_ReturnsEmpty(string? title)
        => Assert.Equal(string.Empty, KaraokeTitleRules.Clean(title, null));
}
