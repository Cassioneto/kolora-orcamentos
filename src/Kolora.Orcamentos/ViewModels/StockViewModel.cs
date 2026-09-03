using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Kolora.Orcamentos.Data;
using Kolora.Orcamentos.Models.Entities;
using Kolora.Orcamentos.Services;

namespace Kolora.Orcamentos.ViewModels;
public partial class StockViewModel : BaseViewModel
{
    private readonly GraficaIdService _grafica;
    [ObservableProperty] private ObservableCollection<Material> _materiais = new();
    [ObservableProperty] private ObservableCollection<Material> _criticos = new();
    public StockViewModel(GraficaIdService grafica) { _grafica = grafica; _ = LoadAsync(); }
    [RelayCommand] public async Task LoadAsync()
    {
        using var db = new KoloraDbContext(_grafica.DbPath);
        var all = await db.Materiais.Where(m => m.GraficaId == _grafica.GraficaId).OrderBy(m => m.Nome).ToListAsync();
        Materiais = new ObservableCollection<Material>(all);
        Criticos = new ObservableCollection<Material>(all.Where(m => m.StockAtual <= m.StockMinimo));
    }
}
