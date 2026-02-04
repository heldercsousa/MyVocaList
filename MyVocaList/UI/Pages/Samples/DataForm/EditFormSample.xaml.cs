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