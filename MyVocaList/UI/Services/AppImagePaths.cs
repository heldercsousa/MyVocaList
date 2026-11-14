namespace MyVocaList.UI.Services;

/// <summary>Where uploaded pictures live: <c>images/</c> inside the app-private data directory (REQ-PIC-09).</summary>
public static class AppImagePaths
{
    public static string ImagesRoot => Path.Combine(FileSystem.AppDataDirectory, "images");
}
