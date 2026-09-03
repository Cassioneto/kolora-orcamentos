using System.Windows.Input;
namespace Kolora.Orcamentos.Helpers;
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
    public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    public bool CanExecute(object? p) => _canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => _execute(p);
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
