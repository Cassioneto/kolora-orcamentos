namespace Kolora.Orcamentos.Data;

using Kolora.Orcamentos.Data.Repositories;
using Microsoft.EntityFrameworkCore;

public interface IUnitOfWork : IDisposable
{
    IGraficaRepository Graficas { get; }
    IProdutoRepository Produtos { get; }
    IMaterialRepository Materiais { get; }
    IClienteRepository Clientes { get; }
    IOrcamentoRepository Orcamentos { get; }
    IPedidoOrcamentoRepository PedidosOrcamento { get; }
    IOutboxRepository OutboxEvents { get; }
    ISyncStateRepository SyncState { get; }
    Task<int> CompleteAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly KoloraDbContext _context;
    private bool _disposed;

    public UnitOfWork(KoloraDbContext context)
    {
        _context = context;
        Graficas = new EfGraficaRepository(_context);
        Produtos = new EfProdutoRepository(_context);
        Materiais = new EfMaterialRepository(_context);
        Clientes = new EfClienteRepository(_context);
        Orcamentos = new EfOrcamentoRepository(_context);
        PedidosOrcamento = new EfPedidoOrcamentoRepository(_context);
        OutboxEvents = new EfOutboxRepository(_context);
        SyncState = new EfSyncStateRepository(_context);
    }

    public IGraficaRepository Graficas { get; }
    public IProdutoRepository Produtos { get; }
    public IMaterialRepository Materiais { get; }
    public IClienteRepository Clientes { get; }
    public IOrcamentoRepository Orcamentos { get; }
    public IPedidoOrcamentoRepository PedidosOrcamento { get; }
    public IOutboxRepository OutboxEvents { get; }
    public ISyncStateRepository SyncState { get; }

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}