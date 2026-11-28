using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyVocaList.Services;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Hosts the official YouTube IFrame player for one video id (REQ-MSRC-07). Only the id is held; title and
/// thumbnail come from the player at runtime. <see cref="StopCommand"/> clears the source so nothing plays in the background.
/// </summary>
[QueryProperty(nameof(VideoId), "v")]
public sealed partial class YouTubePlayerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _videoId = string.Empty;

    [ObservableProperty]
    private string _html = string.Empty;

    partial void OnVideoIdChanged(string value)
    {
        try
        {
            Html = YouTubePlayerHtmlBuilder.Build(value);
        }
        catch (ArgumentException)
        {
            Html = string.Empty;
        }
    }

    [RelayCommand]
    private void Stop() => Html = string.Empty;
}
