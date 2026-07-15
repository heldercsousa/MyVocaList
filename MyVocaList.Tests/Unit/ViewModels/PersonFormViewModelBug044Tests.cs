using MyVocaList.Navigation;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Regression tests for BUG-044: selecting an autocomplete suggestion on the New Singer form
/// navigated by PUSHING a second PersonFormPage on top of the New Singer form. After saving the
/// edit form, "GoToAsync(..)" revealed the stale New Singer form (pre-filled with the raw typed
/// text) instead of the singers list, and saving it inserted a duplicate entity.
/// The fix routes suggestion selection through INavigationService with a REPLACING relative route
/// ("../person-form?...") so the current form page is popped before the edit form is pushed.
/// </summary>
public class PersonFormViewModelBug044Tests
{
    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<PersonFormViewModel>> _loggerMock = new();

    private PersonFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

    private static AutocompleteSuggestion SuggestionFor(Person person) =>
        new(person.FullName, person.GetDisplayIdentifier(), person);

    // [AC] persons AC-2.3: When the user taps a suggestion, the app shall navigate to the Edit
    // Singer form pre-populated with that singer's data. (BUG-044: the navigation must REPLACE
    // the current form page in the Shell stack, never stack a second form on top of it.)
    [Fact]
    public async Task SuggestionSelected_NavigatesWithReplacingRelativeRoute_NotAStackedPush()
    {
        var sut = CreateSut();
        sut.PersonName = "Helder Sousa"; // raw typed text on the New Singer form

        var person = new Person { Id = 7, FullName = "Helder Sousa", BirthdayDayMonth = "15/03", Email = "h@x.com" };
        string navigatedRoute = null;
        _navigationMock.Setup(n => n.GoToAsync(It.IsAny<string>()))
                       .Callback<string>(r => navigatedRoute = r)
                       .Returns(Task.CompletedTask);

        await sut.SuggestionSelectedCommand.ExecuteAsync(SuggestionFor(person));

        Assert.NotNull(navigatedRoute); // navigation must go through the mockable seam
        Assert.StartsWith($"../{Routes.PersonForm}?", navigatedRoute); // "../" pops the current form first
        Assert.Contains("personId=7", navigatedRoute);
        Assert.Contains($"personName={Uri.EscapeDataString(person.FullName)}", navigatedRoute);
    }

    // [AC] persons AC-2.3 (edge): a suggestion whose Data is not a Person must not navigate at all.
    [Fact]
    public async Task SuggestionSelected_NonPersonData_DoesNotNavigate()
    {
        var sut = CreateSut();

        await sut.SuggestionSelectedCommand.ExecuteAsync(new AutocompleteSuggestion("x", "y", "not-a-person"));

        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
