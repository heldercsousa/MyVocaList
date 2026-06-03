namespace MyVocaList.UI.ViewModels;

public sealed partial class FeedbackViewModel : ViewModelBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ISnackbarComponent _snackbar;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool _isSubmitting;

    [ObservableProperty]
    private FeedbackCategory _selectedCategory = FeedbackCategory.BugReport;

    public IReadOnlyList<FeedbackCategory> Categories { get; } =
        Enum.GetValues<FeedbackCategory>().ToList().AsReadOnly();

    public FeedbackViewModel(IFeedbackService feedbackService, ISnackbarComponent snackbar)
    {
        _feedbackService = feedbackService;
        _snackbar = snackbar;
    }

    private bool CanSubmit => !IsSubmitting && !string.IsNullOrWhiteSpace(Message);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        IsSubmitting = true;
        try
        {
            var submission = new FeedbackSubmission(
                SelectedCategory,
                Message.Trim(),
                string.IsNullOrWhiteSpace(Email) ? null : Email.Trim());

            var (success, error) = await _feedbackService.SubmitAsync(submission);

            if (success)
            {
                Message = string.Empty;
                Email   = string.Empty;
                SelectedCategory = FeedbackCategory.BugReport;
                await _snackbar.ShowSuccessAsync("Feedback sent — thank you!");
            }
            else
            {
                await _snackbar.ShowErrorAsync(error ?? "Could not send — please try again");
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
