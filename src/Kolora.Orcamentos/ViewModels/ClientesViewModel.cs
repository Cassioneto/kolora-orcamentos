using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Models.Enums;
using Kolora.Orcamentos.Services;
using Serilog;

namespace Kolora.Orcamentos.ViewModels;
public partial class ClientesViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private ObservableCollection<Cliente> _clientes = new();
    [ObservableProperty] private Cliente? _selecionado;
    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private string _telefone = "";
    [ObservableProperty] private string _filtro = "";
    public ClientesViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var q = db.Clientes.Where(c => c.GraficaId == _grafica.GraficaId);
        if (!string.IsNullOrWhiteSpace(Filtro)) q = q.Where(c => c.Nome.Contains(Filtro) || (c.Telefone != null && c.Telefone.Contains(Filtro)));
        Clientes = new ObservableCollection<Cliente>(await q.OrderBy(c => c.Nome).ToListAsync());
    }
    partial void OnFiltroChanged(string value) => _ = LoadAsync();
    [RelayCommand] public void Novo() { Selecionado = null; Nome = ""; Telefone = ""; }
    [RelayCommand] public void Editar(Cliente c) { Selecionado = c; Nome = c.Nome; Telefone = c.Telefone ?? ""; }
    [RelayCommand] public async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) return;
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            Cliente cli; string op;
            if (Selecionado == null) { cli = new Cliente { Id = Guid.NewGuid(), GraficaId = _grafica.GraficaId, Nome = Nome.Trim(), Telefone = Telefone.Trim(), CriadoEm = DateTime.UtcNow, AtualizadoEm = DateTime.UtcNow }; db.Clientes.Add(cli); op = "INSERT"; }
            else { cli = await db.Clientes.FindAsync(Selecionado.Id) ?? Selecionado; cli.Nome = Nome.Trim(); cli.Telefone = Telefone.Trim(); cli.AtualizadoEm = DateTime.UtcNow; db.Clientes.Update(cli); op = "UPDATE"; }
            db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Cliente", EntidadeId = cli.Id, TipoOperacao = op == "INSERT" ? TipoOperacaoOutbox.INSERT : TipoOperacaoOutbox.UPDATE, PayloadJson = JsonSerializer.Serialize(cli), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" });
            await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); Novo();
        } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Salvar cliente"); }
    }
    [RelayCommand] public async Task ExcluirAsync(Cliente c)
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        using var tx = await db.Database.BeginTransactionAsync();
        try { var e = await db.Clientes.FindAsync(c.Id); if (e == null) return; db.Clientes.Remove(e); db.OutboxEvents.Add(new OutboxEvent { Id = Guid.NewGuid(), Entidade = "Cliente", EntidadeId = c.Id, TipoOperacao = TipoOperacaoOutbox.DELETE, PayloadJson = JsonSerializer.Serialize(e), CriadoEm = DateTime.UtcNow, StatusSync = "Pendente" }); await db.SaveChangesAsync(); await tx.CommitAsync(); await LoadAsync(); } catch (Exception ex) { await tx.RollbackAsync(); Log.Error(ex, "Excluir cliente"); }
    }
}
