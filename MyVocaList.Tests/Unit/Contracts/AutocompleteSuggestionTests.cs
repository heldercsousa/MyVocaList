using MyVocaList.Contracts.Models;

namespace MyVocaList.Tests.Unit.Contracts;

public class AutocompleteSuggestionTests
{
    [Fact]
    public void Defaults_NoLeadingNoTrailing_AllNullAndFlagsFalse()
    {
        // [AC] REQ-ART-09: an option without a leading element or trailing text renders as before
        var s = new AutocompleteSuggestion("Head", "Sub", new object());

        Assert.Equal(AutocompleteLeadingKind.None, s.LeadingKind);
        Assert.Null(s.ImageUrl);
        Assert.Null(s.PlaceholderIcon);
        Assert.Null(s.TrailingText);
        Assert.False(s.HasLeadingImage);
        Assert.False(s.HasLeadingAvatar);
    }

    [Fact]
    public void LeadingKindImage_SetsOnlyHasLeadingImage()
    {
        // [AC] REQ-ART-06: song options show a leading image
        var s = new AutocompleteSuggestion("Head", "Sub", new object())
        {
            LeadingKind = AutocompleteLeadingKind.Image,
            ImageUrl = "https://x/y.jpg",
            PlaceholderIcon = "music_note_outlined",
            TrailingText = "Album",
        };

        Assert.True(s.HasLeadingImage);
        Assert.False(s.HasLeadingAvatar);
        Assert.Equal("https://x/y.jpg", s.ImageUrl);
        Assert.Equal("music_note_outlined", s.PlaceholderIcon);
        Assert.Equal("Album", s.TrailingText);
    }

    [Fact]
    public void LeadingKindAvatar_SetsOnlyHasLeadingAvatar()
    {
        // [AC] REQ-ART-08: artist options show a leading avatar
        var s = new AutocompleteSuggestion("Head", "Sub", new object())
        {
            LeadingKind = AutocompleteLeadingKind.Avatar,
        };

        Assert.True(s.HasLeadingAvatar);
        Assert.False(s.HasLeadingImage);
    }

    [Fact]
    public void ThreeArgumentConstructor_SetsHeadlineSupportingTextAndData()
    {
        // [AC] REQ-ART-09: existing constructor call sites keep working unchanged
        var data = new object();

        var s = new AutocompleteSuggestion("Head", "Sub", data);

        Assert.Equal("Head", s.Headline);
        Assert.Equal("Sub", s.SupportingText);
        Assert.Same(data, s.Data);
        Assert.False(s.IsCreateNew);
    }
}
