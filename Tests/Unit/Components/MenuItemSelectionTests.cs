using System.Windows.Input;
using MyVocaList.UI.Components.Menus;

namespace MyVocaList.Tests.Unit.Components;

public class MenuItemSelectionTests
{
    // [AC] REQ-OVFM-19: selecting a menu item runs its command before the menu closes.
    [Fact]
    public void Select_RunsCommandBeforeClose()
    {
        var log = new List<string>();
        var command = new FakeCommand(canExecute: true, onExecute: () => log.Add("run"));
        var item = new MenuItemModel("Edit", "edit_outlined", command);

        MenuItemSelection.Select(item, isOpen: () => true, close: () => log.Add("close"), closeOnItemSelected: true);

        Assert.Equal(new[] { "run", "close" }, log);
    }

    // [AC] REQ-OVFM-19: the menu does not close a second time when the command already closed it.
    [Fact]
    public void Select_DoesNotCloseWhenCommandAlreadyClosed()
    {
        var isOpen = true;
        var closeCalls = 0;
        var command = new FakeCommand(canExecute: true, onExecute: () => isOpen = false);
        var item = new MenuItemModel("Delete", "delete_outlined", command);

        MenuItemSelection.Select(item, isOpen: () => isOpen, close: () => closeCalls++, closeOnItemSelected: true);

        Assert.Equal(0, closeCalls);
        Assert.Single(command.Executions);
    }

    // [AC] REQ-OVFM-19: with CloseOnItemSelected false the command runs and the menu stays open.
    [Fact]
    public void Select_DoesNotCloseWhenCloseOnItemSelectedFalse()
    {
        var closeCalls = 0;
        var command = new FakeCommand(canExecute: true);
        var item = new MenuItemModel("Edit", "edit_outlined", command);

        MenuItemSelection.Select(item, isOpen: () => true, close: () => closeCalls++, closeOnItemSelected: false);

        Assert.Single(command.Executions);
        Assert.Equal(0, closeCalls);
    }

    // [AC] REQ-OVFM-19: a command that cannot execute does not run, and the menu still closes.
    [Fact]
    public void Select_CommandCannotExecute_DoesNotRun_StillCloses()
    {
        var closeCalls = 0;
        var command = new FakeCommand(canExecute: false);
        var item = new MenuItemModel("Edit", "edit_outlined", command);

        MenuItemSelection.Select(item, isOpen: () => true, close: () => closeCalls++, closeOnItemSelected: true);

        Assert.Empty(command.Executions);
        Assert.Equal(1, closeCalls);
    }

    // [AC] REQ-OVFM-19: a null command does not throw; the menu still closes.
    [Fact]
    public void Select_NullCommand_DoesNotThrow()
    {
        var closeCalls = 0;
        var item = new MenuItemModel("Edit", "edit_outlined", null!);

        var exception = Record.Exception(() =>
            MenuItemSelection.Select(item, isOpen: () => true, close: () => closeCalls++, closeOnItemSelected: true));

        Assert.Null(exception);
        Assert.Equal(1, closeCalls);
    }

    private sealed class FakeCommand(bool canExecute, Action? onExecute = null) : ICommand
    {
        public List<object?> Executions { get; } = [];

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => canExecute;

        public void Execute(object? parameter)
        {
            Executions.Add(parameter);
            onExecute?.Invoke();
        }
    }
}
