using UraniumUI.Pages;
using System.ComponentModel;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates all fundamental components of UraniumUI framework
/// </summary>
public partial class DesignSystemPage : UraniumContentPage
{
    private SampleFormViewModel _autoFormViewModel = new();

    public DesignSystemPage()
    {
        InitializeComponent();
        InitializeDropdown();
        InitializeAutoFormView();
    }

    /// <summary>
    /// Initialize the Dropdown component with sample data
    /// </summary>
    private void InitializeDropdown()
    {
        var dropdownItems = new List<string>
        {
            "Option 1: UraniumContentPage",
            "Option 2: StatefulContentView",
            "Option 3: GridLayout",
            "Option 4: AutoFormView",
            "Option 5: Dropdown",
            "Option 6: ExpanderView",
            "Option 7: Validations"
        };

        //SampleDropdown.ItemsSource = dropdownItems;

        //// UraniumUI Dropdown uses PropertyChanged, not SelectedIndexChanged
        //SampleDropdown.PropertyChanged += OnDropdownPropertyChanged;
    }

    /// <summary>
    /// Initialize AutoFormView with sample ViewModel
    /// </summary>
    private void InitializeAutoFormView()
    {
        _autoFormViewModel = new SampleFormViewModel
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Age = 25,
            PreferredGenre = MusicGenre.Rock,
            SubscribeToNewsletter = true
        };

        //AutoFormDemo.Source = _autoFormViewModel;
    }

    /// <summary>
    /// Handle dropdown selection changes via PropertyChanged event
    /// </summary>
    private void OnDropdownPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    //    if (e.PropertyName == nameof(SampleDropdown.SelectedItem) && SampleDropdown.SelectedItem != null)
    //    {
    //        DropdownSelectedLabel.Text = $"Selected: {SampleDropdown.SelectedItem}";
    //    }
    }

    /// <summary>
    /// Display AutoFormView data in an alert
    /// </summary>
    private async void OnShowAutoFormData(object? sender, EventArgs e)
    {
        var message = $"Full Name: {_autoFormViewModel.FullName}\n" +
                     $"Email: {_autoFormViewModel.Email}\n" +
                     $"Phone: {_autoFormViewModel.PhoneNumber}\n" +
                     $"Age: {_autoFormViewModel.Age}\n" +
                     $"Birth Date: {_autoFormViewModel.BirthDate?.ToString("d")}\n" +
                     $"Preferred Time: {_autoFormViewModel.PreferredContactTime}\n" +
                     $"Genre: {_autoFormViewModel.PreferredGenre}\n" +
                     $"Newsletter: {_autoFormViewModel.SubscribeToNewsletter}\n" +
                     $"Songs: {_autoFormViewModel.NumberOfSongs}";

        await DisplayAlert("AutoFormView Data", message, "OK");
    }

    /// <summary>
    /// Handle form submission
    /// </summary>
    private async void OnFormSubmit(object? sender, EventArgs e)
    {
        //var message = $"Form submitted successfully!\n\n" +
        //             $"Name: {FormName.Text}\n" +
        //             $"Email: {FormEmail.Text}\n" +
        //             $"Message: {FormMessage.Text}\n" +
        //             $"Terms Accepted: {FormTerms.IsChecked}";

        //await DisplayAlert("Success", message, "OK");

        //// Clear form
        //FormName.Text = string.Empty;
        //FormEmail.Text = string.Empty;
        //FormMessage.Text = string.Empty;
        //FormTerms.IsChecked = false;
    }
}