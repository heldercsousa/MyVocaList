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
}
