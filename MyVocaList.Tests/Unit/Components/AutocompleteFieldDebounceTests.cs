using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteFieldDebounceTests
{
    [Fact]
    public async Task Trigger_AfterDelay_InvokesCallback()
    {
        var results = new List<string>();
        var debouncer = new AutocompleteDebouncer(action => action());

        debouncer.Trigger("jo", 50, t => results.Add(t));

        await Task.Delay(150);

        Assert.Single(results);
        Assert.Equal("jo", results[0]);
    }

    [Fact]
    public async Task Trigger_RapidCalls_OnlyLastCallbackFires()
    {
        var results = new List<string>();
        var debouncer = new AutocompleteDebouncer(action => action());

        debouncer.Trigger("j",    100, t => results.Add(t));
        debouncer.Trigger("jo",   100, t => results.Add(t));
        debouncer.Trigger("joh",  100, t => results.Add(t));
        debouncer.Trigger("john", 100, t => results.Add(t));

        await Task.Delay(300);

        Assert.Single(results);
        Assert.Equal("john", results[0]);
    }

    [Fact]
    public async Task Trigger_NullCallback_DoesNotThrow()
    {
        var debouncer = new AutocompleteDebouncer(action => action());

        var ex = await Record.ExceptionAsync(async () =>
        {
            debouncer.Trigger("test", 50, null!);
            await Task.Delay(150);
        });

        Assert.Null(ex);
    }
}
