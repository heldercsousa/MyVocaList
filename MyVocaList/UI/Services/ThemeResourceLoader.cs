namespace MyVocaList.UI.Services;

/// <summary>
/// Loads theme colors from MaterialColors.xaml for MDC configuration
/// </summary>
public static class ThemeResourceLoader
{
    private static ResourceDictionary? _colorResources;

    /// <summary>
    /// Gets color from MaterialColors.xaml by key name
    /// </summary>
    public static Color GetColor(string key)
    {
        EnsureResourcesLoaded();

        if (_colorResources?.TryGetValue(key, out var value) == true && value is Color color)
            return color;

        return Colors.Transparent;
    }

    private static void EnsureResourcesLoaded()
    {
        if (_colorResources != null)
            return;

        _colorResources = new ResourceDictionary();
        _colorResources.LoadFromXaml(GetMaterialColorsXaml());
    }

    private static string GetMaterialColorsXaml()
    {
        var assembly = typeof(ThemeResourceLoader).Assembly;
        var resourceName = "MyVocaList.Resources.Styles.MaterialColors.xaml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\" />";

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
