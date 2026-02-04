# DevExpress CollectionView Samples for MyVocaList

## Sample 1: SwipeContainerSample.xaml
Complete swipe-to-action implementation for queue management.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.CollectionView"
             x:Class="MyVocaList.UI.Pages.Samples.CollectionView.SwipeContainerSample"
             Title="Swipe Actions Sample">
    
    <ContentPage.BindingContext>
        <local:SwipeContainerSampleViewModel/>
    </ContentPage.BindingContext>

    <ContentPage.Resources>
        <Style TargetType="dxcv:SwipeItem">
            <Setter Property="FontColor" Value="White"/>
            <Setter Property="FontSize" Value="Medium"/>
        </Style>
        
        <Style x:Key="SeparatorStyle" TargetType="BoxView">
            <Setter Property="Color" Value="{StaticResource Outline}"/>
            <Setter Property="HeightRequest" Value="1"/>
        </Style>
    </ContentPage.Resources>

    <Grid>
        <dxcv:DXCollectionView ItemsSource="{Binding QueueItems}"
                               Margin="0">
            <dxcv:DXCollectionView.ItemTemplate>
                <DataTemplate>
                    <dxcv:SwipeContainer>
                        <!-- Main Item View -->
                        <dxcv:SwipeContainer.ItemView>
                            <Grid Padding="16,12" 
                                  BackgroundColor="{Binding ItemColor}"
                                  MinimumHeightRequest="72">
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>
                                
                                <Label Text="{Binding SongTitle}" 
                                       FontFamily="RobotoMedium"
                                       FontSize="16"
                                       TextColor="{StaticResource OnSurface}"
                                       Grid.Row="0"/>
                                
                                <Label Text="{Binding SingerName}" 
                                       FontFamily="RobotoRegular"
                                       FontSize="14"
                                       TextColor="{StaticResource OnSurfaceVariant}"
                                       Grid.Row="1"/>
                                
                                <BoxView Style="{StaticResource SeparatorStyle}" 
                                         VerticalOptions="End" 
                                         Grid.RowSpan="2"/>
                            </Grid>
                        </dxcv:SwipeContainer.ItemView>

                        <!-- Start Swipe (Left) - Play Now -->
                        <dxcv:SwipeContainer.StartSwipeItems>
                            <dxcv:SwipeContainerItem 
                                Caption="{Binding ActionText}"
                                BackgroundColor="#4CAF50"
                                Image="{Binding ActionIcon}"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type local:SwipeContainerSampleViewModel}}, Path=ToggleSongStateCommand}"
                                CommandParameter="{Binding .}"/>
                        </dxcv:SwipeContainer.StartSwipeItems>

                        <!-- End Swipe (Right) - Remove -->
                        <dxcv:SwipeContainer.EndSwipeItems>
                            <dxcv:SwipeContainerItem 
                                Caption="Remove"
                                BackgroundColor="#F44336"
                                Image="delete_icon.png"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type local:SwipeContainerSampleViewModel}}, Path=RemoveSongCommand}"
                                CommandParameter="{Binding .}"/>
                        </dxcv:SwipeContainer.EndSwipeItems>
                    </dxcv:SwipeContainer>
                </DataTemplate>
            </dxcv:DXCollectionView.ItemTemplate>
        </dxcv:DXCollectionView>
    </Grid>
</ContentPage>
```

## Sample 1: SwipeContainerSample.xaml.cs

```csharp
using System.Diagnostics;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public partial class SwipeContainerSample : ContentPage
{
    public SwipeContainerSample()
    {
        InitializeComponent();
    }
}
```

## Sample 1: SwipeContainerSampleViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class SwipeContainerSampleViewModel : INotifyPropertyChanged
{
    public ObservableCollection<QueueItem> QueueItems { get; }
    public ICommand ToggleSongStateCommand { get; }
    public ICommand RemoveSongCommand { get; }

    public SwipeContainerSampleViewModel()
    {
        QueueItems = new ObservableCollection<QueueItem>
        {
            new QueueItem("Bohemian Rhapsody", "John Doe"),
            new QueueItem("Don't Stop Believin'", "Jane Smith"),
            new QueueItem("Sweet Child O' Mine", "Mike Johnson"),
            new QueueItem("Livin' on a Prayer", "Sarah Williams"),
            new QueueItem("Hotel California", "Tom Brown"),
            new QueueItem("Wonderwall", "Emily Davis"),
            new QueueItem("Mr. Brightside", "Chris Wilson"),
            new QueueItem("I Want It That Way", "Lisa Anderson")
        };

        ToggleSongStateCommand = new Command<QueueItem>(ToggleSongState);
        RemoveSongCommand = new Command<QueueItem>(RemoveSong);
    }

    private void ToggleSongState(QueueItem item)
    {
        if (item == null) return;
        
        item.IsPlaying = !item.IsPlaying;
        
        // In real app: trigger playback or queue management
        Console.WriteLine($"Toggled: {item.SongTitle} - Playing: {item.IsPlaying}");
    }

    private void RemoveSong(QueueItem item)
    {
        if (item == null) return;
        
        QueueItems.Remove(item);
        Console.WriteLine($"Removed: {item.SongTitle}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class QueueItem : INotifyPropertyChanged
{
    private bool _isPlaying;
    
    public string SongTitle { get; set; }
    public string SingerName { get; set; }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemColor));
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(ActionIcon));
            }
        }
    }

    public Color ItemColor => IsPlaying 
        ? Color.FromArgb("#c6eccb") // Light green for playing
        : Color.FromArgb("#f5f5f5"); // Light gray for queued

    public string ActionText => IsPlaying ? "Queue" : "Play Now";
    public string ActionIcon => IsPlaying ? "pause_icon.png" : "play_icon.png";

    public QueueItem(string songTitle, string singerName)
    {
        SongTitle = songTitle;
        SingerName = singerName;
        IsPlaying = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## Sample 2: DragDropSample.xaml
Drag-and-drop reordering for queue management.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.CollectionView"
             x:Class="MyVocaList.UI.Pages.Samples.CollectionView.DragDropSample"
             Title="Drag & Drop Sample">
    
    <ContentPage.BindingContext>
        <local:DragDropSampleViewModel/>
    </ContentPage.BindingContext>

    <Grid>
        <dxcv:DXCollectionView x:Name="collectionView"
                               ItemsSource="{Binding QueueItems}"
                               AllowDragDropItems="True"
                               DragItem="OnDragItem"
                               DropItem="OnDropItem"
                               CompleteItemDragDrop="OnCompleteItemDragDrop">
            <dxcv:DXCollectionView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding="16,12" 
                          BackgroundColor="{StaticResource Surface}"
                          MinimumHeightRequest="72">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <!-- Drag Handle Icon -->
                        <Image Source="drag_handle.png"
                               WidthRequest="24"
                               HeightRequest="24"
                               VerticalOptions="Center"
                               Margin="0,0,12,0"
                               Grid.Column="0"/>
                        
                        <!-- Song Info -->
                        <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                            <Label Text="{Binding Position, StringFormat='#{0}'}" 
                                   FontFamily="RobotoMedium"
                                   FontSize="12"
                                   TextColor="{StaticResource Primary}"/>
                            
                            <Label Text="{Binding SongTitle}" 
                                   FontFamily="RobotoMedium"
                                   FontSize="16"
                                   TextColor="{StaticResource OnSurface}"/>
                            
                            <Label Text="{Binding SingerName}" 
                                   FontFamily="RobotoRegular"
                                   FontSize="14"
                                   TextColor="{StaticResource OnSurfaceVariant}"/>
                        </VerticalStackLayout>
                        
                        <!-- Duration -->
                        <Label Text="{Binding Duration}"
                               FontFamily="RobotoRegular"
                               FontSize="14"
                               TextColor="{StaticResource OnSurfaceVariant}"
                               VerticalOptions="Center"
                               Grid.Column="2"/>
                        
                        <BoxView Color="{StaticResource Outline}" 
                                 HeightRequest="1" 
                                 VerticalOptions="End" 
                                 Grid.ColumnSpan="3"/>
                    </Grid>
                </DataTemplate>
            </dxcv:DXCollectionView.ItemTemplate>
        </dxcv:DXCollectionView>
    </Grid>
</ContentPage>
```

## Sample 2: DragDropSample.xaml.cs

```csharp
using DevExpress.Maui.CollectionView;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public partial class DragDropSample : ContentPage
{
    private DragDropSampleViewModel ViewModel => (DragDropSampleViewModel)BindingContext;

    public DragDropSample()
    {
        InitializeComponent();
    }

    private void OnDragItem(object? sender, DragItemEventArgs e)
    {
        // Visual feedback during drag
        if (e.Item is DragDropQueueItem draggedItem)
        {
            Console.WriteLine($"Dragging: {draggedItem.SongTitle}");
        }
    }

    private void OnDropItem(object? sender, DropItemEventArgs e)
    {
        // Allow drop if within valid bounds
        e.Allow = e.ToIndex >= 0 && e.ToIndex < ViewModel.QueueItems.Count;
        
        if (e.Item is DragDropQueueItem droppedItem)
        {
            Console.WriteLine($"Dropping: {droppedItem.SongTitle} at position {e.ToIndex}");
        }
    }

    private void OnCompleteItemDragDrop(object? sender, CompleteItemDragDropEventArgs e)
    {
        // Update item positions after drag completes
        ViewModel.UpdatePositions();
        
        if (e.Item is DragDropQueueItem movedItem)
        {
            Console.WriteLine($"Moved: {movedItem.SongTitle} from {e.FromIndex} to {e.ToIndex}");
        }
    }
}
```

## Sample 2: DragDropSampleViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class DragDropSampleViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DragDropQueueItem> QueueItems { get; }

    public DragDropSampleViewModel()
    {
        QueueItems = new ObservableCollection<DragDropQueueItem>
        {
            new DragDropQueueItem(1, "Bohemian Rhapsody", "John Doe", "5:55"),
            new DragDropQueueItem(2, "Don't Stop Believin'", "Jane Smith", "4:10"),
            new DragDropQueueItem(3, "Sweet Child O' Mine", "Mike Johnson", "5:56"),
            new DragDropQueueItem(4, "Livin' on a Prayer", "Sarah Williams", "4:09"),
            new DragDropQueueItem(5, "Hotel California", "Tom Brown", "6:30"),
            new DragDropQueueItem(6, "Wonderwall", "Emily Davis", "4:18"),
            new DragDropQueueItem(7, "Mr. Brightside", "Chris Wilson", "3:42")
        };
    }

    public void UpdatePositions()
    {
        for (int i = 0; i < QueueItems.Count; i++)
        {
            QueueItems[i].Position = i + 1;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DragDropQueueItem : INotifyPropertyChanged
{
    private int _position;

    public int Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged();
            }
        }
    }

    public string SongTitle { get; set; }
    public string SingerName { get; set; }
    public string Duration { get; set; }

    public DragDropQueueItem(int position, string songTitle, string singerName, string duration)
    {
        Position = position;
        SongTitle = songTitle;
        SingerName = singerName;
        Duration = duration;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## Sample 3: FilterChipsSample.xaml
Filtering with ChipGroup for genre selection.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
             xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.CollectionView"
             x:Class="MyVocaList.UI.Pages.Samples.CollectionView.FilterChipsSample"
             Title="Filter Chips Sample">
    
    <ContentPage.BindingContext>
        <local:FilterChipsSampleViewModel/>
    </ContentPage.BindingContext>

    <Grid RowDefinitions="Auto,*" RowSpacing="8">
        <!-- Filter Chips -->
        <dxe:ChipGroup x:Name="genreChips"
                       ItemsSource="{Binding Genres}"
                       DisplayMember="Name"
                       ChipSelected="OnGenreChipSelected"
                       Margin="16,8"
                       Grid.Row="0">
            <dxe:ChipGroup.ChipStyle>
                <Style TargetType="dxe:Chip">
                    <Setter Property="BackgroundColor" Value="{StaticResource SecondaryContainer}"/>
                    <Setter Property="TextColor" Value="{StaticResource OnSecondaryContainer}"/>
                    <Setter Property="SelectedBackgroundColor" Value="{StaticResource Primary}"/>
                    <Setter Property="SelectedTextColor" Value="{StaticResource OnPrimary}"/>
                    <Setter Property="CornerRadius" Value="8"/>
                </Style>
            </dxe:ChipGroup.ChipStyle>
        </dxe:ChipGroup>

        <!-- Filtered Song List -->
        <dxcv:DXCollectionView ItemsSource="{Binding FilteredSongs}"
                               Margin="0"
                               Grid.Row="1">
            <dxcv:DXCollectionView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding="16,12" 
                          BackgroundColor="{StaticResource Surface}"
                          MinimumHeightRequest="56">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <VerticalStackLayout Grid.Column="0" VerticalOptions="Center">
                            <Label Text="{Binding Title}" 
                                   FontFamily="RobotoMedium"
                                   FontSize="16"
                                   TextColor="{StaticResource OnSurface}"/>
                            
                            <Label Text="{Binding Artist}" 
                                   FontFamily="RobotoRegular"
                                   FontSize="14"
                                   TextColor="{StaticResource OnSurfaceVariant}"/>
                        </VerticalStackLayout>
                        
                        <Label Text="{Binding Genre}"
                               FontFamily="RobotoMedium"
                               FontSize="12"
                               TextColor="{StaticResource Primary}"
                               VerticalOptions="Center"
                               Grid.Column="1"/>
                        
                        <BoxView Color="{StaticResource Outline}" 
                                 HeightRequest="1" 
                                 VerticalOptions="End" 
                                 Grid.ColumnSpan="2"/>
                    </Grid>
                </DataTemplate>
            </dxcv:DXCollectionView.ItemTemplate>
        </dxcv:DXCollectionView>
    </Grid>
</ContentPage>
```

## Sample 3: FilterChipsSample.xaml.cs

```csharp
using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public partial class FilterChipsSample : ContentPage
{
    private FilterChipsSampleViewModel ViewModel => (FilterChipsSampleViewModel)BindingContext;

    public FilterChipsSample()
    {
        InitializeComponent();
    }

    private void OnGenreChipSelected(object? sender, ChipEventArgs e)
    {
        if (e.Item is GenreFilter selectedGenre)
        {
            ViewModel.ApplyGenreFilter(selectedGenre.Name);
        }
    }
}
```

## Sample 3: FilterChipsSampleViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class FilterChipsSampleViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Song> _allSongs;
    private ObservableCollection<Song> _filteredSongs;

    public ObservableCollection<GenreFilter> Genres { get; }

    public ObservableCollection<Song> FilteredSongs
    {
        get => _filteredSongs;
        set
        {
            _filteredSongs = value;
            OnPropertyChanged();
        }
    }

    public FilterChipsSampleViewModel()
    {
        // Initialize genres
        Genres = new ObservableCollection<GenreFilter>
        {
            new GenreFilter("All"),
            new GenreFilter("Rock"),
            new GenreFilter("Pop"),
            new GenreFilter("Country"),
            new GenreFilter("R&B"),
            new GenreFilter("Hip-Hop")
        };

        // Initialize songs
        _allSongs = new ObservableCollection<Song>
        {
            new Song("Bohemian Rhapsody", "Queen", "Rock"),
            new Song("Sweet Child O' Mine", "Guns N' Roses", "Rock"),
            new Song("Hotel California", "Eagles", "Rock"),
            new Song("Wonderwall", "Oasis", "Rock"),
            new Song("Don't Stop Believin'", "Journey", "Rock"),
            
            new Song("Billie Jean", "Michael Jackson", "Pop"),
            new Song("I Want It That Way", "Backstreet Boys", "Pop"),
            new Song("Shape of You", "Ed Sheeran", "Pop"),
            new Song("Uptown Funk", "Bruno Mars", "Pop"),
            
            new Song("Jolene", "Dolly Parton", "Country"),
            new Song("Take Me Home, Country Roads", "John Denver", "Country"),
            new Song("Ring of Fire", "Johnny Cash", "Country"),
            
            new Song("Superstition", "Stevie Wonder", "R&B"),
            new Song("Let's Stay Together", "Al Green", "R&B"),
            
            new Song("Lose Yourself", "Eminem", "Hip-Hop"),
            new Song("Juicy", "The Notorious B.I.G.", "Hip-Hop")
        };

        _filteredSongs = new ObservableCollection<Song>(_allSongs);
    }

    public void ApplyGenreFilter(string genreName)
    {
        if (genreName == "All")
        {
            FilteredSongs = new ObservableCollection<Song>(_allSongs);
        }
        else
        {
            FilteredSongs = new ObservableCollection<Song>(
                _allSongs.Where(s => s.Genre == genreName));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GenreFilter
{
    public string Name { get; set; }

    public GenreFilter(string name)
    {
        Name = name;
    }
}

public class Song
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Genre { get; set; }

    public Song(string title, string artist, string genre)
    {
        Title = title;
        Artist = artist;
        Genre = genre;
    }
}
```
