using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Kolora.Orcamentos.Models.Entities;
using Serilog;

namespace Kolora.Orcamentos.Services;

public record OrcamentoPdfModel(Grafica Grafica, ConfiguracoesGrafica Config, Cliente Cliente, List<ItemOrcamentoPdf> Itens, decimal Total, DateTime Validade);
public record ItemOrcamentoPdf(string Nome, string Tipo, decimal Qtd, decimal Largura, decimal Altura, decimal Preco);

public class PdfService
{
    private readonly GraficaIdService _graficaId;
    public PdfService(GraficaIdService graficaId) { _graficaId = graficaId; QuestPDF.Settings.License = LicenseType.Community; }

    public async Task<string> GerarAsync(OrcamentoPdfModel model, Guid orcamentoId)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kolora", "Orcamentos", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
        Directory.CreateDirectory(dir);
        var fileName = "ORC-" + orcamentoId.ToString()[..8].ToUpper() + ".pdf";
        var path = Path.Combine(dir, fileName);
        var logoPath = model.Config.LogoPath;
        if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
            logoPath = Path.Combine(_graficaId.AppDataPath, "assets", "logo.png");

        byte[]? logoBytes = null;
        if (File.Exists(logoPath)) logoBytes = await File.ReadAllBytesAsync(logoPath);

        Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Header().Row(row =>
                {
                    if (logoBytes != null) row.ConstantItem(80).Image(logoBytes);
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(model.Config.NomeExibicaoPdf ?? model.Grafica.Nome).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        if (!string.IsNullOrWhiteSpace(model.Grafica.Telefone)) col.Item().Text("Tel: " + model.Grafica.Telefone).FontSize(9);
                        if (!string.IsNullOrWhiteSpace(model.Grafica.Localizacao)) col.Item().Text(model.Grafica.Localizacao).FontSize(9);
                    });
                    row.ConstantItem(120).AlignRight().Column(col =>
                    {
                        col.Item().Text("ORCAMENTO").FontSize(14).Bold();
                        col.Item().Text("#" + orcamentoId.ToString()[..8].ToUpper()).FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9);
                    });
                });
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().PaddingBottom(8).Column(c2 =>
                    {
                        c2.Item().Text("Cliente: " + model.Cliente.Nome).Bold().FontSize(10);
                        if (!string.IsNullOrWhiteSpace(model.Cliente.Telefone)) c2.Item().Text("Telefone: " + model.Cliente.Telefone).FontSize(9);
                        c2.Item().Text("Validade: " + model.Validade.ToString("dd/MM/yyyy")).FontSize(9);
                    });
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); cd.RelativeColumn(1); cd.RelativeColumn(1); });
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Item").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Qtd").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Medidas").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Preco").Bold().FontSize(9);
                        });
                        foreach (var it in model.Itens)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Nome).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Qtd.ToString("0.##")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(it.Largura > 0 ? it.Largura.ToString("0.##") + "x" + it.Altura.ToString("0.##") + "m" : "-").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(it.Preco.ToString("C", new System.Globalization.CultureInfo("pt-AO"))).FontSize(9);
                        }
                    });
                    col.Item().PaddingTop(10).AlignRight().Text("TOTAL: " + model.Total.ToString("C", new System.Globalization.CultureInfo("pt-AO"))).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                });
                page.Footer().Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(model.Config.MensagemRodapePdf))
                        col.Item().AlignCenter().Text(model.Config.MensagemRodapePdf).FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().Text("KOLORA Gestor - gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor(Colors.Grey.Lighten1);
                });
            });
        }).GeneratePdf(path);
        Log.Information("PDF gerado: {Path}", path);
        return path;
    }
}
