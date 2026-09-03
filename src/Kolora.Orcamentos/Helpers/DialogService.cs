using System.Windows;
namespace Kolora.Orcamentos.Helpers;
public interface IDialogService
{
    void ShowMessage(string msg, string title = "KOLORA");
    bool Confirm(string msg, string title = "Confirmar");
}
public class DialogService : IDialogService
{
    public void ShowMessage(string msg, string title = "KOLORA") => MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
    public bool Confirm(string msg, string title = "Confirmar") => MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
