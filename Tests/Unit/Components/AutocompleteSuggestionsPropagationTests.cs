using MyVocaList.UI.Components.AutocompleteField;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.Tests.Unit.Components;

/// <summary>
/// Regression tests for BUG-043 (suggestions never propagate to the field after the first
/// sub-threshold keystroke). Root cause: <c>HandleTextChanged</c> wrote <c>Suggestions = null</c>
/// directly on the control's own BindableProperty — a manual SetValue on the *target* of the
/// OneWay <c>Suggestions="{Binding Suggestions}"</c> page binding, which removes that binding.
/// Every later ViewModel result assignment then never reached the control.
/// </summary>
public class AutocompleteSuggestionsPropagationTests
{
    public AutocompleteSuggestionsPropagationTests()
    {
        // Binding updates require a dispatcher; install a synchronous one for the test thread.
        Microsoft.Maui.Dispatching.DispatcherProvider.SetCurrent(new SyncDispatcherProvider());
    }

    private sealed class SyncDispatcherProvider : Microsoft.Maui.Dispatching.IDispatcherProvider
    {
        private readonly SyncDispatcher _dispatcher = new();
        public Microsoft.Maui.Dispatching.IDispatcher GetForCurrentThread() => _dispatcher;
    }

    private sealed class SyncDispatcher : Microsoft.Maui.Dispatching.IDispatcher
    {
        public bool IsDispatchRequired => false;
        public bool Dispatch(Action action) { action(); return true; }
        public bool DispatchDelayed(TimeSpan delay, Action action) { action(); return true; }
        public Microsoft.Maui.Dispatching.IDispatcherTimer CreateTimer()
            => throw new NotSupportedException("Timers are not used in these tests.");
    }

    private sealed class FakeSuggestionsVm : INotifyPropertyChanged
    {
        private IEnumerable<AutocompleteSuggestion> _suggestions = [];

        public IEnumerable<AutocompleteSuggestion> Suggestions
        {
            get => _suggestions;
            set { _suggestions = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static AutocompleteField CreateBoundField(FakeSuggestionsVm vm)
    {
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            BindingContext = vm
        };
        field.SetBinding(AutocompleteField.SuggestionsProperty,
            new Binding(nameof(FakeSuggestionsVm.Suggestions)));
        return field;
    }

    // [AC] BUG-043 regression: after a sub-threshold input short-circuit, ViewModel suggestion
    // results must still propagate to the field (the OneWay binding must survive the clear path).
    [Fact]
    public void HandleTextChanged_SubThresholdShortCircuit_DoesNotDetachSuggestionsBinding()
    {
        var vm = new FakeSuggestionsVm();
        var field = CreateBoundField(vm);

        // 1-char input: search gate fails → BUG-043 clear path runs inside the control.
        field.HandleTextChanged("j");

        // ViewModel later assigns a fresh result list (the count=1 case from the probe log).
        vm.Suggestions = [new AutocompleteSuggestion("John Doe", "john@example.com", null)];

        Assert.NotNull(field.Suggestions);
        Assert.Single(field.Suggestions);
    }

    // [AC] BUG-043 regression: repeated short-circuit clears (every keystroke below the gate)
    // must never sever the binding — the exact on-device sequence: "j" → "jo" → results.
    [Fact]
    public void HandleTextChanged_RepeatedShortCircuits_BindingStillPropagatesResults()
    {
        var vm = new FakeSuggestionsVm();
        var field = CreateBoundField(vm);

        field.HandleTextChanged("j");
        field.HandleTextChanged("");
        field.HandleTextChanged("j");

        vm.Suggestions = [new AutocompleteSuggestion("Jane Roe", "jane@example.com", null)];

        Assert.Single(field.Suggestions);
    }
}
