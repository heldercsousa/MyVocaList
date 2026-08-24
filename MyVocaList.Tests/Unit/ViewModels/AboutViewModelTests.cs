using MyVocaList.Contracts;

namespace MyVocaList.Tests.Unit.ViewModels;

public class AboutViewModelTests
{
    // [AC] AC-AB-05c / AC-AB-05d — later year produces a founding-to-current range with an
    // en dash; a same year (or a skewed clock reading earlier) collapses to the single year.
    [Theory]
    [InlineData(2025, 2026, "© 2025–2026 Helder Sousa")]
    [InlineData(2025, 2099, "© 2025–2099 Helder Sousa")]
    [InlineData(2025, 2025, "© 2025 Helder Sousa")]
    [InlineData(2025, 2024, "© 2025 Helder Sousa")]
    public void FormatCopyright_ReturnsExpectedText(int foundedYear, int currentYear, string expected)
    {
        var result = AboutViewModel.FormatCopyright(foundedYear, currentYear);

        Assert.Equal(expected, result);
    }

    // [AC] AC-AB-05e / AC-AB-05f — the line is driven by the founding-year constant, so it
    // never goes stale. The AboutViewModel constructor reads AppInfo.VersionString, a MAUI
    // platform API that throws NotImplementedInReferenceAssemblyException off-device, so the
    // constant-to-formatter wiring is asserted directly rather than through an instance.
    // The remaining property-to-XAML binding step is covered by the manual E2E gate.
    [Fact]
    public void FormatCopyright_WithFoundingConstant_StartsWithFoundingYear()
    {
        var result = AboutViewModel.FormatCopyright(AppConstants.FoundedYear, DateTime.Now.Year);

        Assert.StartsWith($"© {AppConstants.FoundedYear}", result);
    }
}
