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