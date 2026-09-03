using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Models.Entities;

namespace Kolora.Orcamentos.Data;

public class KoloraDbContext : DbContext
{
    public DbSet<Grafica> Graficas => Set<Grafica>();
    public DbSet<ConfiguracoesGrafica> ConfiguracoesGrafica => Set<ConfiguracoesGrafica>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Material> Materiais => Set<Material>();
    public DbSet<ProdutoMaterial> ProdutoMateriais => Set<ProdutoMaterial>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<PedidoOrcamento> PedidosOrcamento => Set<PedidoOrcamento>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<SyncState> SyncState => Set<SyncState>();

    private readonly string? _dbPath;

    public KoloraDbContext(string? dbPath)
    {
        _dbPath = dbPath;
    }

    public KoloraDbContext(DbContextOptions<KoloraDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_dbPath))
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KoloraDbContext).Assembly);
    }
}
