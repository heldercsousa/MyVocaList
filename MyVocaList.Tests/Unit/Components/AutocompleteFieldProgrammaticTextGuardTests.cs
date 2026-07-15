using System.Windows.Input;
using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

/// <summary>
/// Regression tests for BUG-047 (Major): setting <see cref="AutocompleteField.Text"/>
/// programmatically — the way a ViewModel hydrates the bound field when loading an existing
/// entity for edit — must not fire the debounced search or populate stale <see cref="AutocompleteField.Suggestions"/>.
/// Genuine user-originated typing must still trigger the search exactly as before.
/// Root cause: <see cref="AutocompleteField"/>'s <c>TextProperty</c> propertyChanged handler fed
/// every Text write — programmatic or user-typed — into <c>HandleTextChanged</c> unconditionally.
/// <see cref="AutocompleteMobileField"/> already guarded its own analogous reentrancy path
/// (<c>_isUpdatingTextFromSearchEdit</c>); this ports an equivalent guard to the desktop field.
/// </summary>
public class AutocompleteFieldProgrammaticTextGuardTests
{
    private sealed class SpyCommand : ICommand
    {
        public List<object> ExecutedWith { get; } = [];
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => ExecutedWith.Add(parameter);
        public event EventHandler CanExecuteChanged;
    }

    // [AC] BUG-047: a programmatic Text assignment (ViewModel hydration on Edit-mode load) must
    // NOT invoke SearchRequestedCommand.
    [Fact]
    public void SettingTextProgrammatically_DoesNotInvokeSearchRequestedCommand()
    {
        var spy = new SpyCommand();
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            SearchRequestedCommand = spy,
            DebounceDelay = 1
        };
        field.UseSynchronousDebouncerForTests();

        // Simulates PersonFormViewModel hydrating PersonName on Edit-mode load — a direct property
        // set, not user typing.
        field.Text = "Helder Sousa";

        // Give the debounce window a chance to fire in case the guard failed to prevent it.
        Thread.Sleep(200);

        Assert.Empty(spy.ExecutedWith);
    }

    // [AC] BUG-047: genuine user-originated typing (including retyping/correcting the name while
    // already in Edit mode) must still trigger the debounced search — this is not a mode split,
    // it is a reentrancy guard only.
    [Fact]
    public void SetTextFromUserInput_StillInvokesSearchRequestedCommand()
    {
        var spy = new SpyCommand();
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            SearchRequestedCommand = spy,
            DebounceDelay = 1
        };
        // Real debounce firing dispatches via MainThread.BeginInvokeOnMainThread, unavailable
        // outside a running MAUI app — swap in a synchronous dispatcher for deterministic assertion.
        field.UseSynchronousDebouncerForTests();

        field.SetTextFromUserInput("Helder Sousa");

        SpinWaitUntil(() => spy.ExecutedWith.Count > 0, TimeSpan.FromSeconds(2));

        Assert.Single(spy.ExecutedWith);
        Assert.Equal("Helder Sousa", spy.ExecutedWith[0]);
    }

    // [AC] BUG-047: the guard must not break the existing desktop TextEdit sync — Text still
    // reflects the last value assigned regardless of origin.
    [Fact]
    public void SettingTextProgrammatically_StillUpdatesTextProperty()
    {
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            Text = "Helder Sousa"
        };

        Assert.Equal("Helder Sousa", field.Text);
    }

    private static void SpinWaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(20);
        }
    }
}
