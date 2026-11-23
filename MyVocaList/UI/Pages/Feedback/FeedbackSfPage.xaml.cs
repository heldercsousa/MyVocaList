namespace MyVocaList.UI.Pages.Feedback;

public partial class FeedbackSfPage : ContentPage
{
    public FeedbackSfPage(FeedbackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
