using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Services;
using Kolora.Orcamentos.ViewModels;
using Kolora.Orcamentos.Views;
using Microsoft.EntityFrameworkCore;

namespace Kolora.Orcamentos;
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kolora");
        Directory.CreateDirectory(appData);
        Directory.CreateDirectory(Path.Combine(appData, "assets"));
        Directory.CreateDirectory(Path.Combine(appData, "logs"));
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(a => a.File(Path.Combine(appData, "logs", "kolora-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14))
            .CreateLogger();

        // Handlers globais: erros de binding/comando/async deixam de ser silenciosos
        DispatcherUnhandledException += (_, ex) =>
        {
            Log.Error(ex.Exception, "Excecao nao tratada na UI");
            MessageBox.Show("Ocorreu um erro: " + ex.Exception.Message, "KOLORA", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            Log.Fatal(ex.ExceptionObject as Exception, "Excecao nao tratada (dominio)");
        TaskScheduler.UnobservedTaskException += (_, ex) =>
            Log.Error(ex.Exception, "Task nao observada");

        try
        {
            Log.Information("KOLORA Gestor iniciando...");

            _host = Host.CreateDefaultBuilder()
                
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                    if (File.Exists(appSettingsPath)) cfg.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<GraficaIdService>();
                    services.AddSingleton<IGraficaIdService>(sp => sp.GetRequiredService<GraficaIdService>());
                    services.AddSingleton<SupabaseKeyVault>();
                    services.AddSingleton<ConfigurationService>();
                    services.AddSingleton<IConfigurationService>(sp => sp.GetRequiredService<ConfigurationService>());
                    services.AddSingleton<INetworkMonitorService, NetworkMonitorService>();
                    services.AddSingleton<ISupabaseService, SupabaseService>();
                    services.AddSingleton<ISyncService, SyncBackgroundService>();
                    services.AddHostedService(sp => (SyncBackgroundService)sp.GetRequiredService<ISyncService>());
                    services.AddSingleton<CalculadoraService>();
                    services.AddSingleton<StockService>();
                    services.AddSingleton<PdfService>();
                    services.AddSingleton<BackupService>();
                    services.AddSingleton<Helpers.IDialogService, Helpers.DialogService>();

                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ProdutosViewModel>();
                    services.AddTransient<MateriaisViewModel>();
                    services.AddTransient<CalculadoraViewModel>();
                    services.AddTransient<StockViewModel>();
                    services.AddTransient<OrcamentosViewModel>();
                    services.AddTransient<ClientesViewModel>();
                    services.AddTransient<PedidosRecebidosViewModel>();
                    services.AddTransient<ConfiguracoesViewModel>();
                    services.AddTransient<SobreViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            var graficaIdService = _host.Services.GetRequiredService<GraficaIdService>();
            await graficaIdService.EnsureInitializedAsync();

            // Migrations + WAL
            using (var db = new KoloraDbContext(graficaIdService.DbPath))
            {
                await db.Database.MigrateAsync();
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
                // Seed 5 produtos se vazio
                if (!await db.Produtos.AnyAsync())
                {
                    var gid = graficaIdService.GraficaId;
                    var produtos = new[]
                    {
                        new Models.Entities.Produto{ Id=Guid.NewGuid(), GraficaId=gid, Nome="DTF", TipoCalculo=Models.Enums.TipoCalculo.M2, PrecoCustoBase=1500, MargemPadrao=0.40m, Ativo=true, AtualizadoEm=DateTime.UtcNow },
                        new Models.Entities.Produto{ Id=Guid.NewGuid(), GraficaId=gid, Nome="Lona 440g", TipoCalculo=Models.Enums.TipoCalculo.M2, PrecoCustoBase=2500, MargemPadrao=0.45m, Ativo=true, AtualizadoEm=DateTime.UtcNow },
                        new Models.Entities.Produto{ Id=Guid.NewGuid(), GraficaId=gid, Nome="Vinil", TipoCalculo=Models.Enums.TipoCalculo.M2, PrecoCustoBase=3000, MargemPadrao=0.40m, Ativo=true, AtualizadoEm=DateTime.UtcNow },
                        new Models.Entities.Produto{ Id=Guid.NewGuid(), GraficaId=gid, Nome="Cartao de Visita", TipoCalculo=Models.Enums.TipoCalculo.Unidade, PrecoCustoBase=15, MargemPadrao=0.50m, Ativo=true, AtualizadoEm=DateTime.UtcNow },
                        new Models.Entities.Produto{ Id=Guid.NewGuid(), GraficaId=gid, Nome="Sublimacao", TipoCalculo=Models.Enums.TipoCalculo.Unidade, PrecoCustoBase=800, MargemPadrao=0.40m, Ativo=true, AtualizadoEm=DateTime.UtcNow },
                    };
                    db.Produtos.AddRange(produtos);
                    await db.SaveChangesAsync();
                    Log.Information("Seed 5 produtos criado");
                }
            }

            await _host.StartAsync();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Falha no startup");
            MessageBox.Show("Erro ao iniciar: " + ex.Message, "KOLORA", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null) await _host.StopAsync();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
