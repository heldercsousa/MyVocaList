namespace MyVocaList.Tests.Unit.ViewModels;

public class YouTubePlayerViewModelTests
{
    // [AC] REQ-MSRC-07: a valid id yields player html
    [Fact]
    public void SettingVideoId_BuildsHtml()
    {
        var vm = new YouTubePlayerViewModel { VideoId = "dQw4w9WgXcQ" };

        Assert.Contains("dQw4w9WgXcQ", vm.Html);
    }

    // [AC] REQ-MSRC-07: no background play - stop clears the source
    [Fact]
    public void Stop_ClearsSource()
    {
        var vm = new YouTubePlayerViewModel { VideoId = "dQw4w9WgXcQ" };

        vm.StopCommand.Execute(null);

        Assert.Equal(string.Empty, vm.Html);
    }

    // [AC] REQ-MSRC-07: invalid id never reaches the player
    [Fact]
    public void SettingVideoId_Invalid_LeavesHtmlEmpty()
    {
        var vm = new YouTubePlayerViewModel { VideoId = "bad" };

        Assert.Equal(string.Empty, vm.Html);
    }
}
