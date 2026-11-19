using MyVocaList.Infra.Similarity;

namespace MyVocaList.Tests.Unit.Services.Similarity;

/// <summary>
/// Level A unit tests for <see cref="SimilarityScorer"/>.
/// Constructs the scorer directly (no Moq needed — pure managed, no I/O).
/// Threshold assertions reflect the findings.md validated scores.
/// </summary>
public class SimilarityScorerTests
{
    private readonly SimilarityScorer _sut = new();

    // ── Identity baseline ─────────────────────────────────────────────────

    [Fact]
    public void Score_IdenticalStrings_Returns1()
    {
        Assert.Equal(1.0, _sut.Score("Bohemian Rhapsody", "Bohemian Rhapsody"));
    }

    [Fact]
    public void Score_IdenticalStringsAfterNormalization_Returns1()
    {
        // Case variant only — NFD + lowercase makes them identical
        Assert.Equal(1.0, _sut.Score("QUEEN", "queen"));
    }

    // ── Accent / typo tolerance (findings.md confirmed scores) ────────────

    [Fact]
    public void Score_BjorkBiork_AtLeast080()
    {
        // Raw "Björk" vs "Biork" = 0.60; after NFD normalize = 0.80
        var score = _sut.Score("Björk", "Biork");
        Assert.True(score >= 0.80, $"Expected ≥ 0.80 but got {score:F2}");
    }

    [Fact]
    public void Score_NaoSeiAccented_AtLeast095()
    {
        // "Não Sei" vs "Nao Sei" — accent only; spike confirmed 1.00
        var score = _sut.Score("Não Sei", "Nao Sei");
        Assert.True(score >= 0.95, $"Expected ≥ 0.95 but got {score:F2}");
    }

    // ── Unrelated strings should score low ───────────────────────────────

    [Fact]
    public void Score_QueenMadonna_LessThan030()
    {
        // Completely unrelated — spike confirmed 0.17
        var score = _sut.Score("Queen", "Madonna");
        Assert.True(score < 0.30, $"Expected < 0.30 but got {score:F2}");
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Fact]
    public void Score_EmptyA_Returns0()
    {
        Assert.Equal(0.0, _sut.Score("", "Queen"));
    }

    [Fact]
    public void Score_EmptyB_Returns0()
    {
        Assert.Equal(0.0, _sut.Score("Queen", ""));
    }

    [Fact]
    public void Score_BothEmpty_Returns0()
    {
        Assert.Equal(0.0, _sut.Score("", ""));
    }

    [Fact]
    public void Score_NullA_Returns0()
    {
        Assert.Equal(0.0, _sut.Score(null, "Queen"));
    }

    [Fact]
    public void Score_NullB_Returns0()
    {
        Assert.Equal(0.0, _sut.Score("Queen", null));
    }

    // ── Determinism ──────────────────────────────────────────────────────

    [Fact]
    public void Score_SameInputCalledTwice_ReturnsSameValue()
    {
        var score1 = _sut.Score("Bohemian Rhapsody", "Bohemian Rhapsody (Live)");
        var score2 = _sut.Score("Bohemian Rhapsody", "Bohemian Rhapsody (Live)");
        Assert.Equal(score1, score2);
    }

    // ── RankScore (design § 6.3; display ordering only, exception § 6.4) ──

    [Fact]
    public void RankScore_DontCry_RanksDontCryAboveCry()
    {
        // [AC] REQ-SFT-30: a typo-tolerant query ranks the closest title first.
        Assert.True(_sut.RankScore("dont cry", "Don't Cry") > _sut.RankScore("dont cry", "Cry"));
    }

    [Fact]
    public void RankScore_DonCry_RanksDontCryAboveCry()
    {
        // [AC] REQ-SFT-30: a truncated-word query still ranks the closest title first.
        Assert.True(_sut.RankScore("don cry", "Don't Cry") > _sut.RankScore("don cry", "Cry"));
    }

    [Fact]
    public void RankScore_DontCry_RanksDontCryAboveBoysDontCry()
    {
        // [AC] REQ-SFT-30: the exact-token title outranks a longer title containing it.
        Assert.True(_sut.RankScore("dont cry", "Don't Cry") > _sut.RankScore("dont cry", "Boys Don't Cry"));
    }

    [Fact]
    public void RankScore_GunsCry_TitleArtistOfDontCryAboveCryByCyndiLauper()
    {
        // [AC] REQ-SFT-32: artist words in the query rank the matching title+artist first.
        Assert.True(_sut.RankScore("guns cry", "Don't Cry Guns N' Roses") > _sut.RankScore("guns cry", "Cry Cyndi Lauper"));
    }

    [Theory]
    [InlineData("nao sei")]
    [InlineData("NAO SEI")]
    public void RankScore_AccentAndCaseVariant_ScoresOne(string query)
    {
        // [AC] REQ-SFT-31: accents and case do not affect the rank score.
        Assert.Equal(1.0, _sut.RankScore(query, "Não Sei"));
    }

    [Theory]
    [InlineData("dont cry", "Don't Cry")]
    [InlineData("queen", "Madonna")]
    [InlineData("", "Cry")]
    [InlineData("guns cry", "")]
    public void RankScore_Range_IsBetweenZeroAndOne(string query, string candidate)
    {
        // [AC] REQ-SFT-30: the rank score is always within [0, 1].
        var score = _sut.RankScore(query, candidate);
        Assert.True(score >= 0.0 && score <= 1.0, $"Expected within [0,1] but got {score}");
    }
}
