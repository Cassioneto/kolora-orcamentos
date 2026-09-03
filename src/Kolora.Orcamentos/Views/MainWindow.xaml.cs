using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Kolora.Orcamentos.Services;
using Kolora.Orcamentos.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kolora.Orcamentos.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _sp;
    private Button? _botaoAtivo;

    public MainWindow(IServiceProvider sp, ISyncService sync)
    {
        InitializeComponent();
        _sp = sp;
        sync.StatusChanged += (_, s) => Dispatcher.Invoke(() => SyncText.Text = s);
        sync.NovosPedidos += (_, _) => Dispatcher.Invoke(() => AtualizarBadge());
        Loaded += (_, _) => Navigate<DashboardViewModel>(GetButton("BtnNav_Dashboard"));
    }

    private void Navigate<T>(Button botao) where T : BaseViewModel
    {
        try
        {
            var vm = _sp.GetRequiredService<T>();
            MainContent.Content = vm; // DataTemplate em App.xaml cria a View com DataContext certo
            if (botao != null) MarcarAtivo(botao);
            if (typeof(T) == typeof(PedidosRecebidosViewModel)) _ = Task.Run(AtualizarBadge);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erro ao navegar para {Tela}", typeof(T).Name);
            MessageBox.Show("Erro ao abrir a tela: " + ex.Message, "KOLORA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Button GetButton(string name) => (Button)FindName(name);

    private void MarcarAtivo(Button botao)
    {
        if (_botaoAtivo != null) _botaoAtivo.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x21, 0x3E));
        _botaoAtivo = botao;
        botao.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x35));
    }

    private async void AtualizarBadge()
    {
        try
        {
            var grafica = _sp.GetRequiredService<GraficaIdService>();
            using var db = new Data.KoloraDbContext(grafica.DbPath);
            var novos = await db.PedidosOrcamento.CountAsync(p => p.GraficaId == grafica.GraficaId && p.Status == Models.Enums.StatusPedido.Novo);
            Dispatcher.Invoke(() =>
            {
                BadgePedidos.Text = $" {novos}";
                BadgePedidos.Visibility = novos > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }
        catch { /* badge nunca quebra a UI */ }
    }

    private void Nav_Dashboard(object s, RoutedEventArgs e) => Navigate<DashboardViewModel>((Button)s);
    private void Nav_Produtos(object s, RoutedEventArgs e) => Navigate<ProdutosViewModel>((Button)s);
    private void Nav_Materiais(object s, RoutedEventArgs e) => Navigate<MateriaisViewModel>((Button)s);
    private void Nav_Calculadora(object s, RoutedEventArgs e) => Navigate<CalculadoraViewModel>((Button)s);
    private void Nav_Stock(object s, RoutedEventArgs e) => Navigate<StockViewModel>((Button)s);
    private void Nav_Orcamentos(object s, RoutedEventArgs e) => Navigate<OrcamentosViewModel>((Button)s);
    private void Nav_Clientes(object s, RoutedEventArgs e) => Navigate<ClientesViewModel>((Button)s);
    private void Nav_Pedidos(object s, RoutedEventArgs e) => Navigate<PedidosRecebidosViewModel>((Button)s);
    private void Nav_Config(object s, RoutedEventArgs e) => Navigate<ConfiguracoesViewModel>((Button)s);
    private void Nav_Sobre(object s, RoutedEventArgs e) => Navigate<SobreViewModel>((Button)s);
}
