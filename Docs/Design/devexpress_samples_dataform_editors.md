# DevExpress DataForm & Editors Samples for MyVocaList

## Sample 4: EditFormSample.xaml
Complete DataForm with validation for singer/song entry.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxdf="clr-namespace:DevExpress.Maui.DataForm;assembly=DevExpress.Maui.Editors"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.DataForm"
             x:Class="MyVocaList.UI.Pages.Samples.DataForm.EditFormSample"
             Title="Edit Form Sample">
    
    <ContentPage.BindingContext>
        <local:EditFormSampleViewModel/>
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="16">
            <Label Text="Add Singer Information"
                   FontFamily="RobotoMedium"
                   FontSize="24"
                   TextColor="{StaticResource OnSurface}"/>

            <dxdf:DataFormView x:Name="dataForm"
                               DataObject="{Binding Singer}"
                               EditorLabelColor="{StaticResource Primary}"
                               EditorLabelWidth="80"
                               ValidateProperty="OnValidateProperty"
                               CommitMode="PropertyChanged">

                <!-- First Name -->
                <dxdf:DataFormTextItem FieldName="FirstName"
                                       LabelText="First Name"
                                       IsInplaceLabelFloating="True"
                                       InplaceLabelText="Enter first name"/>

                <!-- Last Name -->
                <dxdf:DataFormTextItem FieldName="LastName"
                                       LabelText="Last Name"
                                       IsInplaceLabelFloating="True"
                                       InplaceLabelText="Enter last name"/>

                <!-- Email -->
                <dxdf:DataFormTextItem FieldName="Email"
                                       LabelText="Email"
                                       IsInplaceLabelFloating="True"
                                       InplaceLabelText="Enter email address"
                                       Keyboard="Email"/>

                <!-- Phone -->
                <dxdf:DataFormTextItem FieldName="Phone"
                                       LabelText="Phone"
                                       IsInplaceLabelFloating="True"
                                       InplaceLabelText="Enter phone number"
                                       Keyboard="Telephone"/>

                <!-- Preferred Genre (ComboBox) -->
                <dxdf:DataFormComboBoxItem FieldName="PreferredGenre"
                                           LabelText="Genre"
                                           IsInplaceLabelFloating="True"
                                           InplaceLabelText="Select preferred genre"
                                           PickerSourceProvider="{x:Static local:GenrePickerProvider.Instance}"/>

                <!-- Notes -->
                <dxdf:DataFormMultilineItem FieldName="Notes"
                                            LabelText="Notes"
                                            IsInplaceLabelFloating="True"
                                            InplaceLabelText="Additional notes"
                                            EditorHeight="100"/>

                <!-- Active Status -->
                <dxdf:DataFormSwitchItem FieldName="IsActive"
                                         LabelText="Active"/>
            </dxdf:DataFormView>

            <!-- Action Buttons -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="16" Margin="0,16,0,0">
                <Button Text="Save"
                        StyleClass="FilledButton"
                        Command="{Binding SaveCommand}"
                        Grid.Column="0"/>

                <Button Text="Cancel"
                        StyleClass="OutlinedButton"
                        Command="{Binding CancelCommand}"
                        Grid.Column="1"/>
            </Grid>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## Sample 4: EditFormSample.xaml.cs

```csharp
using DevExpress.Maui.DataForm;
using System.Net.Mail;

namespace MyVocaList.UI.Pages.Samples.DataForm;

public partial class EditFormSample : ContentPage
{
    public EditFormSample()
    {
        InitializeComponent();
    }

    private void OnValidateProperty(object? sender, DataFormPropertyValidationEventArgs e)
    {
        // Email validation
        if (e.PropertyName == nameof(Singer.Email) && e.NewValue != null)
        {
            var emailValue = e.NewValue.ToString();
            if (!string.IsNullOrWhiteSpace(emailValue) && 
                !MailAddress.TryCreate(emailValue, out _))
            {
                e.HasError = true;
                e.ErrorText = "Invalid email address";
            }
        }

        // Phone validation (simple format check)
        if (e.PropertyName == nameof(Singer.Phone) && e.NewValue != null)
        {
            var phoneValue = e.NewValue.ToString();
            if (!string.IsNullOrWhiteSpace(phoneValue))
            {
                var digitsOnly = new string(phoneValue.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length < 10)
                {
                    e.HasError = true;
                    e.ErrorText = "Phone must have at least 10 digits";
                }
            }
        }
    }
}
```

## Sample 4: EditFormSampleViewModel.cs

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.DataForm;

public class EditFormSampleViewModel : INotifyPropertyChanged
{
    public Singer Singer { get; set; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditFormSampleViewModel()
    {
        Singer = new Singer();
        SaveCommand = new Command(OnSave);
        CancelCommand = new Command(OnCancel);
    }

    private async void OnSave()
    {
        // In real app: save to database via service
        await Application.Current!.MainPage!.DisplayAlert(
            "Success", 
            $"Singer {Singer.FirstName} {Singer.LastName} saved!", 
            "OK");
        
        Console.WriteLine($"Saved Singer: {Singer.FirstName} {Singer.LastName}");
    }

    private async void OnCancel()
    {
        var confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Cancel", 
            "Discard changes?", 
            "Yes", 
            "No");
        
        if (confirmed)
        {
            Singer = new Singer();
            OnPropertyChanged(nameof(Singer));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class Singer : INotifyPropertyChanged
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _preferredGenre = string.Empty;
    private string _notes = string.Empty;
    private bool _isActive = true;

    [Required(ErrorMessage = "First name is required")]
    [MinLength(2, ErrorMessage = "First name must be at least 2 characters")]
    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    [Required(ErrorMessage = "Last name is required")]
    [MinLength(2, ErrorMessage = "Last name must be at least 2 characters")]
    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string PreferredGenre
    {
        get => _preferredGenre;
        set => SetProperty(ref _preferredGenre, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

// ComboBox Data Provider
public class GenrePickerProvider : IPickerSourceProvider
{
    public static GenrePickerProvider Instance { get; } = new GenrePickerProvider();

    public IList<string> GetSource()
    {
        return new List<string>
        {
            "Rock",
            "Pop",
            "Country",
            "R&B",
            "Hip-Hop",
            "Jazz",
            "Blues",
            "Electronic",
            "Classical",
            "Alternative"
        };
    }
}
```

## Sample 5: ComboBoxEditorSample.xaml
Advanced ComboBox with complex object binding.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxdf="clr-namespace:DevExpress.Maui.DataForm;assembly=DevExpress.Maui.Editors"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.DataForm"
             x:Class="MyVocaList.UI.Pages.Samples.DataForm.ComboBoxEditorSample"
             Title="ComboBox Editor Sample">
    
    <ContentPage.BindingContext>
        <local:ComboBoxEditorSampleViewModel/>
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="16">
            <Label Text="Song Selection Form"
                   FontFamily="RobotoMedium"
                   FontSize="24"
                   TextColor="{StaticResource OnSurface}"/>

            <dxdf:DataFormView DataObject="{Binding Request}"
                               EditorLabelColor="{StaticResource Primary}"
                               EditorLabelWidth="80">

                <!-- Singer Name (Simple String ComboBox) -->
                <dxdf:DataFormComboBoxItem FieldName="SingerName"
                                           LabelText="Singer"
                                           IsInplaceLabelFloating="True"
                                           InplaceLabelText="Select or enter singer name"
                                           IsTextEditable="True"
                                           PickerSourceProvider="{x:Static local:SingerNamePickerProvider.Instance}"/>

                <!-- Song (Complex Object ComboBox) -->
                <dxdf:DataFormComboBoxItem FieldName="SelectedSongId"
                                           LabelText="Song"
                                           IsInplaceLabelFloating="True"
                                           InplaceLabelText="Select a song"
                                           PickerSourceProvider="{x:Static local:SongPickerProvider.Instance}"
                                           ValueMember="Id"
                                           DisplayMember="FullTitle"/>

                <!-- Urgency Level (Enum ComboBox) -->
                <dxdf:DataFormComboBoxItem FieldName="Urgency"
                                           LabelText="Urgency"
                                           IsInplaceLabelFloating="True"
                                           InplaceLabelText="Select urgency level"
                                           PickerSourceProvider="{x:Static local:UrgencyPickerProvider.Instance}"/>

                <!-- Special Requests -->
                <dxdf:DataFormMultilineItem FieldName="SpecialRequests"
                                            LabelText="Notes"
                                            IsInplaceLabelFloating="True"
                                            InplaceLabelText="Any special requests?"
                                            EditorHeight="80"/>
            </dxdf:DataFormView>

            <!-- Submit Button -->
            <Button Text="Submit Request"
                    StyleClass="FilledButton"
                    Command="{Binding SubmitCommand}"
                    Margin="0,16,0,0"/>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## Sample 5: ComboBoxEditorSample.xaml.cs

```csharp
namespace MyVocaList.UI.Pages.Samples.DataForm;

public partial class ComboBoxEditorSample : ContentPage
{
    public ComboBoxEditorSample()
    {
        InitializeComponent();
    }
}
```

## Sample 5: ComboBoxEditorSampleViewModel.cs

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.DataForm;

public class ComboBoxEditorSampleViewModel : INotifyPropertyChanged
{
    public SongRequest Request { get; set; }
    public ICommand SubmitCommand { get; }

    public ComboBoxEditorSampleViewModel()
    {
        Request = new SongRequest();
        SubmitCommand = new Command(OnSubmit);
    }

    private async void OnSubmit()
    {
        var selectedSong = SongPickerProvider.Instance.GetSource()
            .FirstOrDefault(s => s.Id == Request.SelectedSongId);

        await Application.Current!.MainPage!.DisplayAlert(
            "Request Submitted",
            $"Singer: {Request.SingerName}\n" +
            $"Song: {selectedSong?.FullTitle ?? "N/A"}\n" +
            $"Urgency: {Request.Urgency}",
            "OK");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SongRequest : INotifyPropertyChanged
{
    private string _singerName = string.Empty;
    private int _selectedSongId;
    private UrgencyLevel _urgency = UrgencyLevel.Normal;
    private string _specialRequests = string.Empty;

    public string SingerName
    {
        get => _singerName;
        set => SetProperty(ref _singerName, value);
    }

    public int SelectedSongId
    {
        get => _selectedSongId;
        set => SetProperty(ref _selectedSongId, value);
    }

    public UrgencyLevel Urgency
    {
        get => _urgency;
        set => SetProperty(ref _urgency, value);
    }

    public string SpecialRequests
    {
        get => _specialRequests;
        set => SetProperty(ref _specialRequests, value);
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

public class SongInfo
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Genre { get; set; }
    public string FullTitle => $"{Title} - {Artist}";

    public SongInfo(int id, string title, string artist, string genre)
    {
        Id = id;
        Title = title;
        Artist = artist;
        Genre = genre;
    }
}

public enum UrgencyLevel
{
    Low,
    Normal,
    High,
    VIP
}

// Picker Providers
public class SingerNamePickerProvider : IPickerSourceProvider
{
    public static SingerNamePickerProvider Instance { get; } = new SingerNamePickerProvider();

    public IList<string> GetSource()
    {
        return new List<string>
        {
            "John Doe",
            "Jane Smith",
            "Mike Johnson",
            "Sarah Williams",
            "Tom Brown",
            "Emily Davis",
            "Chris Wilson",
            "Lisa Anderson"
        };
    }
}

public class SongPickerProvider : IPickerSourceProvider
{
    public static SongPickerProvider Instance { get; } = new SongPickerProvider();

    public IList<SongInfo> GetSource()
    {
        return new List<SongInfo>
        {
            new SongInfo(1, "Bohemian Rhapsody", "Queen", "Rock"),
            new SongInfo(2, "Don't Stop Believin'", "Journey", "Rock"),
            new SongInfo(3, "Sweet Child O' Mine", "Guns N' Roses", "Rock"),
            new SongInfo(4, "Hotel California", "Eagles", "Rock"),
            new SongInfo(5, "Billie Jean", "Michael Jackson", "Pop"),
            new SongInfo(6, "I Want It That Way", "Backstreet Boys", "Pop"),
            new SongInfo(7, "Shape of You", "Ed Sheeran", "Pop"),
            new SongInfo(8, "Jolene", "Dolly Parton", "Country"),
            new SongInfo(9, "Superstition", "Stevie Wonder", "R&B"),
            new SongInfo(10, "Lose Yourself", "Eminem", "Hip-Hop")
        };
    }
}

public class UrgencyPickerProvider : IPickerSourceProvider
{
    public static UrgencyPickerProvider Instance { get; } = new UrgencyPickerProvider();

    public IList<UrgencyLevel> GetSource()
    {
        return new List<UrgencyLevel>
        {
            UrgencyLevel.Low,
            UrgencyLevel.Normal,
            UrgencyLevel.High,
            UrgencyLevel.VIP
        };
    }
}
```

## Sample 6: EditorsSample.xaml
Standalone editors showcase (without DataForm).

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
             xmlns:local="clr-namespace:MyVocaList.UI.Pages.Samples.Editors"
             x:Class="MyVocaList.UI.Pages.Samples.Editors.EditorsSample"
             Title="Editors Sample">
    
    <ContentPage.BindingContext>
        <local:EditorsSampleViewModel/>
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="24">
            <Label Text="DevExpress Editors"
                   FontFamily="RobotoMedium"
                   FontSize="24"
                   TextColor="{StaticResource OnSurface}"/>

            <!-- TextEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Text Input" StyleClass="Label.Medium"/>
                <dxe:TextEdit Text="{Binding TextValue}"
                              LabelText="Song Title"
                              BoxMode="Outlined"
                              BoxCornerRadius="4"
                              FocusedBorderColor="{StaticResource Primary}"
                              BorderColor="{StaticResource Outline}"/>
            </VerticalStackLayout>

            <!-- PasswordEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Password Input" StyleClass="Label.Medium"/>
                <dxe:PasswordEdit Text="{Binding PasswordValue}"
                                  LabelText="Password"
                                  BoxMode="Outlined"
                                  BoxCornerRadius="4"
                                  FocusedBorderColor="{StaticResource Primary}"
                                  BorderColor="{StaticResource Outline}"/>
            </VerticalStackLayout>

            <!-- ComboBoxEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="ComboBox Selection" StyleClass="Label.Medium"/>
                <dxe:ComboBoxEdit ItemsSource="{Binding GenreList}"
                                  SelectedItem="{Binding SelectedGenre}"
                                  LabelText="Genre"
                                  BoxMode="Outlined"
                                  BoxCornerRadius="4"
                                  FocusedBorderColor="{StaticResource Primary}"
                                  BorderColor="{StaticResource Outline}"/>
            </VerticalStackLayout>

            <!-- NumericEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Numeric Input" StyleClass="Label.Medium"/>
                <dxe:NumericEdit Value="{Binding QueuePosition}"
                                 LabelText="Queue Position"
                                 BoxMode="Outlined"
                                 BoxCornerRadius="4"
                                 FocusedBorderColor="{StaticResource Primary}"
                                 BorderColor="{StaticResource Outline}"
                                 MinValue="1"
                                 MaxValue="99"/>
            </VerticalStackLayout>

            <!-- DateEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Date Selection" StyleClass="Label.Medium"/>
                <dxe:DateEdit Date="{Binding EventDate}"
                              LabelText="Event Date"
                              BoxMode="Outlined"
                              BoxCornerRadius="4"
                              FocusedBorderColor="{StaticResource Primary}"
                              BorderColor="{StaticResource Outline}"/>
            </VerticalStackLayout>

            <!-- TimeEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Time Selection" StyleClass="Label.Medium"/>
                <dxe:TimeEdit Time="{Binding EventTime}"
                              LabelText="Start Time"
                              BoxMode="Outlined"
                              BoxCornerRadius="4"
                              FocusedBorderColor="{StaticResource Primary}"
                              BorderColor="{StaticResource Outline}"/>
            </VerticalStackLayout>

            <!-- CheckEdit -->
            <HorizontalStackLayout Spacing="12">
                <dxe:CheckEdit IsChecked="{Binding IsVIP}"
                               CheckColor="{StaticResource Primary}"/>
                <Label Text="VIP Priority" 
                       StyleClass="Body.Medium" 
                       VerticalOptions="Center"/>
            </HorizontalStackLayout>

            <!-- MultilineEdit -->
            <VerticalStackLayout Spacing="4">
                <Label Text="Multiline Input" StyleClass="Label.Medium"/>
                <dxe:MultilineEdit Text="{Binding Notes}"
                                   LabelText="Additional Notes"
                                   BoxMode="Outlined"
                                   BoxCornerRadius="4"
                                   FocusedBorderColor="{StaticResource Primary}"
                                   BorderColor="{StaticResource Outline}"
                                   MinHeight="100"/>
            </VerticalStackLayout>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## Sample 6: EditorsSample.xaml.cs

```csharp
namespace MyVocaList.UI.Pages.Samples.Editors;

public partial class EditorsSample : ContentPage
{
    public EditorsSample()
    {
        InitializeComponent();
    }
}
```

## Sample 6: EditorsSampleViewModel.cs

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.Editors;

public class EditorsSampleViewModel : INotifyPropertyChanged
{
    private string _textValue = string.Empty;
    private string _passwordValue = string.Empty;
    private string _selectedGenre = "Rock";
    private int _queuePosition = 1;
    private DateTime _eventDate = DateTime.Now;
    private TimeSpan _eventTime = DateTime.Now.TimeOfDay;
    private bool _isVIP = false;
    private string _notes = string.Empty;

    public ObservableCollection<string> GenreList { get; }

    public string TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public string PasswordValue
    {
        get => _passwordValue;
        set => SetProperty(ref _passwordValue, value);
    }

    public string SelectedGenre
    {
        get => _selectedGenre;
        set => SetProperty(ref _selectedGenre, value);
    }

    public int QueuePosition
    {
        get => _queuePosition;
        set => SetProperty(ref _queuePosition, value);
    }

    public DateTime EventDate
    {
        get => _eventDate;
        set => SetProperty(ref _eventDate, value);
    }

    public TimeSpan EventTime
    {
        get => _eventTime;
        set => SetProperty(ref _eventTime, value);
    }

    public bool IsVIP
    {
        get => _isVIP;
        set => SetProperty(ref _isVIP, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public EditorsSampleViewModel()
    {
        GenreList = new ObservableCollection<string>
        {
            "Rock", "Pop", "Country", "R&B", "Hip-Hop", 
            "Jazz", "Blues", "Electronic", "Classical"
        };
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
