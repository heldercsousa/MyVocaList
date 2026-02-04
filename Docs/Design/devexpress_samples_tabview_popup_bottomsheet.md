# DevExpress TabView, Popup & BottomSheet Samples for MyVocaList

## Sample 7: BottomTabViewSample.xaml
Bottom navigation with TabView for main app navigation.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.Controls;assembly=DevExpress.Maui.Controls"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.TabView"
             x:Class="MyVocaList.UI.Pages.Samples.TabView.BottomTabViewSample"
             Title="Bottom TabView Sample">
    
    <ContentPage.BindingContext>
        <local:BottomTabViewSampleViewModel/>
    </ContentPage.BindingContext>

    <dxcv:TabView x:Name="tabView"
                  ItemsSource="{Binding Tabs}"
                  ItemHeaderWidth="*"
                  HeaderPanelPosition="Bottom"
                  HeaderPanelBackgroundColor="{StaticResource SurfaceContainer}"
                  SelectedItemChanged="OnSelectedItemChanged">
        
        <!-- Header Template -->
        <dxcv:TabView.ItemHeaderTemplate>
            <DataTemplate>
                <Grid RowDefinitions="Auto,Auto" 
                      RowSpacing="4"
                      Padding="8">
                    <Image Source="{Binding Icon}"
                           WidthRequest="24"
                           HeightRequest="24"
                           HorizontalOptions="Center"
                           Grid.Row="0"/>
                    
                    <Label Text="{Binding Title}"
                           FontFamily="RobotoMedium"
                           FontSize="12"
                           HorizontalOptions="Center"
                           TextColor="{StaticResource OnSurfaceVariant}"
                           Grid.Row="1"/>
                </Grid>
            </DataTemplate>
        </dxcv:TabView.ItemHeaderTemplate>

        <!-- Content Template -->
        <dxcv:TabView.ItemTemplate>
            <DataTemplate>
                <ContentView>
                    <Grid Padding="16">
                        <VerticalStackLayout Spacing="16" VerticalOptions="Center">
                            <Image Source="{Binding ContentIcon}"
                                   WidthRequest="64"
                                   HeightRequest="64"
                                   HorizontalOptions="Center"/>
                            
                            <Label Text="{Binding Title}"
                                   FontFamily="RobotoMedium"
                                   FontSize="24"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource OnSurface}"/>
                            
                            <Label Text="{Binding Description}"
                                   FontFamily="RobotoRegular"
                                   FontSize="16"
                                   HorizontalOptions="Center"
                                   TextColor="{StaticResource OnSurfaceVariant}"
                                   HorizontalTextAlignment="Center"/>
                        </VerticalStackLayout>
                    </Grid>
                </ContentView>
            </DataTemplate>
        </dxcv:TabView.ItemTemplate>
    </dxcv:TabView>
</ContentPage>
```

## Sample 7: BottomTabViewSample.xaml.cs

```csharp
using DevExpress.Maui.Controls;

namespace MyVocaList.UI.Pages.Samples.TabView;

public partial class BottomTabViewSample : ContentPage
{
    public BottomTabViewSample()
    {
        InitializeComponent();
    }

    private void OnSelectedItemChanged(object? sender, TabViewSelectedItemChangedEventArgs e)
    {
        if (e.NewItem is TabItem selectedTab)
        {
            Console.WriteLine($"Selected Tab: {selectedTab.Title}");
        }
    }
}
```

## Sample 7: BottomTabViewSampleViewModel.cs

```csharp
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
```

## Sample 8: PopupServiceSample.xaml
Popup service for dialogs and confirmations.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dx="http://schemas.devexpress.com/maui"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.Popup"
             x:Class="MyVocaList.UI.Pages.Samples.Popup.PopupServiceSample"
             Title="Popup Service Sample">
    
    <ContentPage.BindingContext>
        <local:PopupServiceSampleViewModel/>
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="16">
            <Label Text="Popup Samples"
                   FontFamily="RobotoMedium"
                   FontSize="24"
                   TextColor="{StaticResource OnSurface}"/>

            <!-- Simple Alert -->
            <Button Text="Show Simple Alert"
                    StyleClass="FilledButton"
                    Command="{Binding ShowSimpleAlertCommand}"/>

            <!-- Confirmation Dialog -->
            <Button Text="Show Confirmation"
                    StyleClass="FilledTonalButton"
                    Command="{Binding ShowConfirmationCommand}"/>

            <!-- Custom Content Popup -->
            <Button Text="Show Custom Popup"
                    StyleClass="OutlinedButton"
                    Command="{Binding ShowCustomPopupCommand}"/>

            <!-- Result Display -->
            <Frame BackgroundColor="{StaticResource SurfaceContainerHighest}"
                   CornerRadius="12"
                   Padding="16"
                   IsVisible="{Binding HasResult}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Last Action:"
                           FontFamily="RobotoMedium"
                           FontSize="14"
                           TextColor="{StaticResource Primary}"/>
                    
                    <Label Text="{Binding LastResult}"
                           FontFamily="RobotoRegular"
                           FontSize="14"
                           TextColor="{StaticResource OnSurface}"/>
                </VerticalStackLayout>
            </Frame>
        </VerticalStackLayout>
    </ScrollView>

    <!-- Custom Popup Definition -->
    <ContentPage.Resources>
        <dx:DXPopup x:Name="customPopup"
                    AllowScrim="True"
                    CornerRadius="28"
                    ShadowRadius="8">
            <dx:DXPopup.Content>
                <Grid Padding="24" 
                      BackgroundColor="{StaticResource Surface}"
                      WidthRequest="300"
                      RowDefinitions="Auto,Auto,Auto,Auto"
                      RowSpacing="16">
                    
                    <Label Text="Add to Queue?"
                           FontFamily="RobotoMedium"
                           FontSize="20"
                           TextColor="{StaticResource OnSurface}"
                           Grid.Row="0"/>
                    
                    <Label Text="This will add 'Bohemian Rhapsody' to the end of the queue."
                           FontFamily="RobotoRegular"
                           FontSize="14"
                           TextColor="{StaticResource OnSurfaceVariant}"
                           Grid.Row="1"/>
                    
                    <HorizontalStackLayout Spacing="12" 
                                          HorizontalOptions="End"
                                          Grid.Row="2">
                        <dx:DXButton Content="Cancel"
                                     StyleClass="TextButton"
                                     Clicked="OnCustomPopupCancel"/>
                        
                        <dx:DXButton Content="Add"
                                     StyleClass="FilledButton"
                                     Clicked="OnCustomPopupConfirm"/>
                    </HorizontalStackLayout>
                </Grid>
            </dx:DXPopup.Content>
        </dx:DXPopup>
    </ContentPage.Resources>
</ContentPage>
```

## Sample 8: PopupServiceSample.xaml.cs

```csharp
namespace MyVocaList.UI.Pages.Samples.Popup;

public partial class PopupServiceSample : ContentPage
{
    private PopupServiceSampleViewModel ViewModel => (PopupServiceSampleViewModel)BindingContext;

    public PopupServiceSample()
    {
        InitializeComponent();
    }

    private void OnCustomPopupCancel(object? sender, EventArgs e)
    {
        customPopup.IsOpen = false;
        ViewModel.LastResult = "Popup cancelled";
        ViewModel.HasResult = true;
    }

    private void OnCustomPopupConfirm(object? sender, EventArgs e)
    {
        customPopup.IsOpen = false;
        ViewModel.LastResult = "Song added to queue!";
        ViewModel.HasResult = true;
    }

    public void ShowCustomPopup()
    {
        customPopup.IsOpen = true;
    }
}
```

## Sample 8: PopupServiceSampleViewModel.cs

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.Popup;

public class PopupServiceSampleViewModel : INotifyPropertyChanged
{
    private string _lastResult = string.Empty;
    private bool _hasResult = false;

    public string LastResult
    {
        get => _lastResult;
        set => SetProperty(ref _lastResult, value);
    }

    public bool HasResult
    {
        get => _hasResult;
        set => SetProperty(ref _hasResult, value);
    }

    public ICommand ShowSimpleAlertCommand { get; }
    public ICommand ShowConfirmationCommand { get; }
    public ICommand ShowCustomPopupCommand { get; }

    public PopupServiceSampleViewModel()
    {
        ShowSimpleAlertCommand = new Command(ShowSimpleAlert);
        ShowConfirmationCommand = new Command(ShowConfirmation);
        ShowCustomPopupCommand = new Command(ShowCustomPopup);
    }

    private async void ShowSimpleAlert()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Information",
            "This is a simple alert dialog.",
            "OK");
        
        LastResult = "Simple alert dismissed";
        HasResult = true;
    }

    private async void ShowConfirmation()
    {
        bool result = await Application.Current!.MainPage!.DisplayAlert(
            "Confirm Action",
            "Do you want to remove this song from the queue?",
            "Yes",
            "No");
        
        LastResult = result ? "User confirmed" : "User cancelled";
        HasResult = true;
    }

    private void ShowCustomPopup()
    {
        // Trigger custom popup through code-behind
        if (Application.Current?.MainPage is NavigationPage navPage &&
            navPage.CurrentPage is PopupServiceSample page)
        {
            page.ShowCustomPopup();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## Sample 9: BottomSheetSample.xaml
BottomSheet for filters and contextual actions.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dx="http://schemas.devexpress.com/maui"
             xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.BottomSheet"
             x:Class="MyVocaList.UI.Pages.Samples.BottomSheet.BottomSheetSample"
             Title="BottomSheet Sample">
    
    <ContentPage.BindingContext>
        <local:BottomSheetSampleViewModel/>
    </ContentPage.BindingContext>

    <Grid>
        <!-- Main Content -->
        <VerticalStackLayout Padding="16" Spacing="16">
            <Label Text="BottomSheet Samples"
                   FontFamily="RobotoMedium"
                   FontSize="24"
                   TextColor="{StaticResource OnSurface}"/>

            <Button Text="Show Filter BottomSheet"
                    StyleClass="FilledButton"
                    Clicked="OnShowFilterBottomSheet"/>

            <Button Text="Show Action BottomSheet"
                    StyleClass="FilledTonalButton"
                    Clicked="OnShowActionBottomSheet"/>

            <!-- Applied Filters Display -->
            <Frame BackgroundColor="{StaticResource SurfaceContainerHighest}"
                   CornerRadius="12"
                   Padding="16"
                   IsVisible="{Binding HasFilters}">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Active Filters:"
                           FontFamily="RobotoMedium"
                           FontSize="14"
                           TextColor="{StaticResource Primary}"/>
                    
                    <Label Text="{Binding FilterSummary}"
                           FontFamily="RobotoRegular"
                           FontSize="14"
                           TextColor="{StaticResource OnSurface}"/>
                </VerticalStackLayout>
            </Frame>
        </VerticalStackLayout>

        <!-- Filter BottomSheet -->
        <dx:BottomSheet x:Name="filterBottomSheet"
                        AllowDismiss="True"
                        CornerRadius="28"
                        HalfExpandedRatio="0.5"
                        State="Hidden">
            <Grid Padding="24" 
                  BackgroundColor="{StaticResource Surface}"
                  RowDefinitions="Auto,Auto,Auto,Auto,Auto"
                  RowSpacing="16">
                
                <!-- Handle -->
                <BoxView WidthRequest="32"
                         HeightRequest="4"
                         CornerRadius="2"
                         BackgroundColor="{StaticResource OnSurfaceVariant}"
                         HorizontalOptions="Center"
                         Opacity="0.4"
                         Grid.Row="0"/>
                
                <Label Text="Filter Songs"
                       FontFamily="RobotoMedium"
                       FontSize="20"
                       TextColor="{StaticResource OnSurface}"
                       Margin="0,8,0,0"
                       Grid.Row="1"/>
                
                <!-- Genre Chips -->
                <VerticalStackLayout Spacing="8" Grid.Row="2">
                    <Label Text="Genre" 
                           FontFamily="RobotoMedium"
                           FontSize="14"
                           TextColor="{StaticResource OnSurfaceVariant}"/>
                    
                    <dxe:ChipGroup ItemsSource="{Binding Genres}"
                                   DisplayMember="Name"
                                   ChipSelected="OnGenreChipSelected">
                        <dxe:ChipGroup.ChipStyle>
                            <Style TargetType="dxe:Chip">
                                <Setter Property="BackgroundColor" Value="{StaticResource SecondaryContainer}"/>
                                <Setter Property="TextColor" Value="{StaticResource OnSecondaryContainer}"/>
                                <Setter Property="SelectedBackgroundColor" Value="{StaticResource Primary}"/>
                                <Setter Property="SelectedTextColor" Value="{StaticResource OnPrimary}"/>
                            </Style>
                        </dxe:ChipGroup.ChipStyle>
                    </dxe:ChipGroup>
                </VerticalStackLayout>
                
                <!-- Year Range -->
                <VerticalStackLayout Spacing="8" Grid.Row="3">
                    <Label Text="Release Year" 
                           FontFamily="RobotoMedium"
                           FontSize="14"
                           TextColor="{StaticResource OnSurfaceVariant}"/>
                    
                    <Slider Minimum="1950"
                            Maximum="2024"
                            Value="{Binding SelectedYear}"
                            MinimumTrackColor="{StaticResource Primary}"
                            MaximumTrackColor="{StaticResource SurfaceContainerHighest}"/>
                    
                    <Label Text="{Binding SelectedYear, StringFormat='Year: {0:F0}'}"
                           FontFamily="RobotoRegular"
                           FontSize="12"
                           TextColor="{StaticResource OnSurfaceVariant}"/>
                </VerticalStackLayout>
                
                <!-- Action Buttons -->
                <Grid ColumnDefinitions="*,*" 
                      ColumnSpacing="12"
                      Grid.Row="4">
                    <dx:DXButton Content="Clear"
                                 StyleClass="OutlinedButton"
                                 Clicked="OnClearFilters"
                                 Grid.Column="0"/>
                    
                    <dx:DXButton Content="Apply"
                                 StyleClass="FilledButton"
                                 Clicked="OnApplyFilters"
                                 Grid.Column="1"/>
                </Grid>
            </Grid>
        </dx:BottomSheet>

        <!-- Action BottomSheet -->
        <dx:BottomSheet x:Name="actionBottomSheet"
                        AllowDismiss="True"
                        CornerRadius="28"
                        State="Hidden">
            <VerticalStackLayout Padding="24" 
                                 BackgroundColor="{StaticResource Surface}"
                                 Spacing="0">
                
                <!-- Handle -->
                <BoxView WidthRequest="32"
                         HeightRequest="4"
                         CornerRadius="2"
                         BackgroundColor="{StaticResource OnSurfaceVariant}"
                         HorizontalOptions="Center"
                         Opacity="0.4"
                         Margin="0,0,0,16"/>
                
                <!-- Action Items -->
                <dx:DXButton Content="Play Next"
                             Icon="play_next_icon.png"
                             StyleClass="TextButton"
                             HorizontalContentAlignment="Start"
                             Padding="16,12"
                             Clicked="OnActionPlay"/>
                
                <dx:DXButton Content="Add to Favorites"
                             Icon="favorite_icon.png"
                             StyleClass="TextButton"
                             HorizontalContentAlignment="Start"
                             Padding="16,12"
                             Clicked="OnActionFavorite"/>
                
                <dx:DXButton Content="Share"
                             Icon="share_icon.png"
                             StyleClass="TextButton"
                             HorizontalContentAlignment="Start"
                             Padding="16,12"
                             Clicked="OnActionShare"/>
                
                <dx:DXButton Content="Remove from Queue"
                             Icon="delete_icon.png"
                             StyleClass="TextButton"
                             HorizontalContentAlignment="Start"
                             Padding="16,12"
                             TextColor="{StaticResource Error}"
                             Clicked="OnActionRemove"/>
            </VerticalStackLayout>
        </dx:BottomSheet>
    </Grid>
</ContentPage>
```

## Sample 9: BottomSheetSample.xaml.cs

```csharp
using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Pages.Samples.BottomSheet;

public partial class BottomSheetSample : ContentPage
{
    private BottomSheetSampleViewModel ViewModel => (BottomSheetSampleViewModel)BindingContext;

    public BottomSheetSample()
    {
        InitializeComponent();
    }

    private void OnShowFilterBottomSheet(object? sender, EventArgs e)
    {
        filterBottomSheet.State = BottomSheetState.HalfExpanded;
    }

    private void OnShowActionBottomSheet(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.FullExpanded;
    }

    private void OnGenreChipSelected(object? sender, ChipEventArgs e)
    {
        if (e.Item is GenreItem genre)
        {
            ViewModel.SelectedGenre = genre.Name;
        }
    }

    private void OnClearFilters(object? sender, EventArgs e)
    {
        ViewModel.ClearFilters();
        filterBottomSheet.State = BottomSheetState.Hidden;
    }

    private void OnApplyFilters(object? sender, EventArgs e)
    {
        ViewModel.ApplyFilters();
        filterBottomSheet.State = BottomSheetState.Hidden;
    }

    private async void OnActionPlay(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Playing next...", "OK");
    }

    private async void OnActionFavorite(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Added to favorites!", "OK");
    }

    private async void OnActionShare(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Sharing...", "OK");
    }

    private async void OnActionRemove(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        bool confirmed = await DisplayAlert("Confirm", "Remove from queue?", "Yes", "No");
        if (confirmed)
        {
            await DisplayAlert("Removed", "Song removed from queue", "OK");
        }
    }
}
```

## Sample 9: BottomSheetSampleViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.BottomSheet;

public class BottomSheetSampleViewModel : INotifyPropertyChanged
{
    private string _selectedGenre = "All";
    private double _selectedYear = 2024;
    private bool _hasFilters = false;
    private string _filterSummary = string.Empty;

    public ObservableCollection<GenreItem> Genres { get; }

    public string SelectedGenre
    {
        get => _selectedGenre;
        set => SetProperty(ref _selectedGenre, value);
    }

    public double SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public bool HasFilters
    {
        get => _hasFilters;
        set => SetProperty(ref _hasFilters, value);
    }

    public string FilterSummary
    {
        get => _filterSummary;
        set => SetProperty(ref _filterSummary, value);
    }

    public BottomSheetSampleViewModel()
    {
        Genres = new ObservableCollection<GenreItem>
        {
            new GenreItem("All"),
            new GenreItem("Rock"),
            new GenreItem("Pop"),
            new GenreItem("Country"),
            new GenreItem("R&B"),
            new GenreItem("Hip-Hop"),
            new GenreItem("Jazz")
        };
    }

    public void ApplyFilters()
    {
        FilterSummary = $"Genre: {SelectedGenre}, Year: {SelectedYear:F0}";
        HasFilters = true;
    }

    public void ClearFilters()
    {
        SelectedGenre = "All";
        SelectedYear = 2024;
        HasFilters = false;
        FilterSummary = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GenreItem
{
    public string Name { get; set; }

    public GenreItem(string name)
    {
        Name = name;
    }
}
```
