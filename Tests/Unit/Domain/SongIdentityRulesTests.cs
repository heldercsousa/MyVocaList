using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class SongIdentityRulesTests
{
    // [AC] REQ-SLD-12: The artist of a Deezer song is shown as plain text.
    [Fact]
    public void IsDeezerSong_ProviderAndIdSet_True()
    {
        Assert.True(SongIdentityRules.IsDeezerSong("Deezer", "123"));
    }

    // [AC] REQ-SLD-13: The artist of a local-only song stays editable.
    [Theory]
    [InlineData(null, "123")]
    [InlineData("", "123")]
    [InlineData(" ", "123")]
    [InlineData("Deezer", null)]
    [InlineData("Deezer", "")]
    [InlineData("Deezer", " ")]
    [InlineData(null, null)]
    public void IsDeezerSong_EitherBlank_False(string? provider, string? id)
    {
        Assert.False(SongIdentityRules.IsDeezerSong(provider, id));
    }
}
