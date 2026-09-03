using System.Windows;
using Kolora.Orcamentos.Services;
using Kolora.Orcamentos.ViewModels;
namespace Kolora.Orcamentos.Views;
public partial class MainWindow : Window
{
    private readonly IServiceProvider _sp;
    public MainWindow(IServiceProvider sp, ISyncService sync)
    {
        InitializeComponent();
        _sp = sp;
        sync.StatusChanged += (_, s) => Dispatcher.Invoke(() => SyncText.Text = s);
        Loaded += (_, __) => Navigate<DashboardViewModel>();
    }
    private void Navigate<T>() where T : BaseViewModel
    {
        var vm = _sp.GetService(typeof(T)) as BaseViewModel;
        var viewType = Type.GetType($"Kolora.Orcamentos.Views.{typeof(T).Name.Replace("ViewModel","View")}");
        if (viewType != null) { var view = Activator.CreateInstance(viewType) as FrameworkElement; if(view!=null){ view.DataContext = vm; MainContent.Content = view; } }
        else MainContent.Content = vm;
    }
    private void Nav_Dashboard(object s, RoutedEventArgs e) => Navigate<DashboardViewModel>();
    private void Nav_Produtos(object s, RoutedEventArgs e) => Navigate<ProdutosViewModel>();
    private void Nav_Materiais(object s, RoutedEventArgs e) => Navigate<MateriaisViewModel>();
    private void Nav_Calculadora(object s, RoutedEventArgs e) => Navigate<CalculadoraViewModel>();
    private void Nav_Stock(object s, RoutedEventArgs e) => Navigate<StockViewModel>();
    private void Nav_Orcamentos(object s, RoutedEventArgs e) => Navigate<OrcamentosViewModel>();
    private void Nav_Clientes(object s, RoutedEventArgs e) => Navigate<ClientesViewModel>();
    private void Nav_Pedidos(object s, RoutedEventArgs e) => Navigate<PedidosRecebidosViewModel>();
    private void Nav_Config(object s, RoutedEventArgs e) => Navigate<ConfiguracoesViewModel>();
    private void Nav_Sobre(object s, RoutedEventArgs e) => Navigate<SobreViewModel>();
}
