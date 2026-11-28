using System.Text.RegularExpressions;

namespace MyVocaList.Services;

/// <summary>
/// Builds the page that hosts the official YouTube IFrame player (REQ-MSRC-07). Nothing is drawn over the player,
/// the player shows its own title and branding, and playback stays inline.
/// </summary>
public static partial class YouTubePlayerHtmlBuilder
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex IdPattern();

    public static string Build(string videoId)
    {
        if (string.IsNullOrEmpty(videoId) || !IdPattern().IsMatch(videoId))
            throw new ArgumentException("A YouTube video id has exactly 11 characters (letters, digits, _ or -).", nameof(videoId));

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>html,body{margin:0;height:100%;background:#000}#player{width:100%;height:100%}</style>
            </head>
            <body>
            <div id="player"></div>
            <script src="https://www.youtube.com/iframe_api"></script>
            <script>
            function onYouTubeIframeAPIReady() {
              new YT.Player('player', {
                width: '100%',
                height: '100%',
                videoId: '{{videoId}}',
                playerVars: { playsinline: 1, rel: 0 }
              });
            }
            </script>
            </body>
            </html>
            """;
    }
}
