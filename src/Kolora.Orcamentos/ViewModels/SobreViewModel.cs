using CommunityToolkit.Mvvm.ComponentModel;
namespace Kolora.Orcamentos.ViewModels;
public partial class SobreViewModel : BaseViewModel
{
    public string Versao => "1.0.0";
    public string Info => "KOLORA Gestor — Offline-first para gráficas de Luanda.\n.NET 8 + WPF + SQLite (WAL) + QuestPDF + Supabase.\nSingle-user, sync assíncrono, PDF offline.\n\nFase 1 entregue. Roadmap em ROADMAP.md";
}
