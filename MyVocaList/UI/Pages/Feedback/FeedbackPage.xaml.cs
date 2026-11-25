namespace MyVocaList.UI.Pages.Feedback;

public partial class FeedbackPage : ContentPage
{
    public FeedbackPage(FeedbackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
