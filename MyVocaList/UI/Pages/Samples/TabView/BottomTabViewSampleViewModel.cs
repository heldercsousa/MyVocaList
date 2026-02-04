using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.TabView;

public class BottomTabViewSampleViewModel : INotifyPropertyChanged
{
    public ObservableCollection<TabItem> Tabs { get; }

    public BottomTabViewSampleViewModel()
    {
        Tabs = new ObservableCollection<TabItem>
        {
            new TabItem(
                "Queue",
                "queue_icon.png",
                "queue_large.png",
                "Manage your song queue"),

            new TabItem(
                "Singers",
                "singers_icon.png",
                "singers_large.png",
                "Browse singer profiles"),

            new TabItem(
                "Songs",
                "songs_icon.png",
                "songs_large.png",
                "Search song library"),

            new TabItem(
                "History",
                "history_icon.png",
                "history_large.png",
                "View past performances"),

            new TabItem(
                "Settings",
                "settings_icon.png",
                "settings_large.png",
                "App configuration")
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TabItem
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public string ContentIcon { get; set; }
    public string Description { get; set; }

    public TabItem(string title, string icon, string contentIcon, string description)
    {
        Title = title;
        Icon = icon;
        ContentIcon = contentIcon;
        Description = description;
    }
}