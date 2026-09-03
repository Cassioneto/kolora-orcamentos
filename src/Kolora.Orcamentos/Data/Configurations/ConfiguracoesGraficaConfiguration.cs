namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class ConfiguracoesGraficaConfiguration : IEntityTypeConfiguration<ConfiguracoesGrafica>
{
    public void Configure(EntityTypeBuilder<ConfiguracoesGrafica> b)
    {
        b.HasKey(c => c.Id);
        b.HasIndex(c => c.GraficaId).IsUnique();
        b.Property(c => c.LogoPath).HasMaxLength(500);
        b.Property(c => c.NomeExibicaoPdf).HasMaxLength(200);
        b.Property(c => c.MensagemRodapePdf).HasMaxLength(500);
        b.Property(c => c.MargemPadraoGlobal).HasColumnType("decimal(18,4)");
        b.Property(c => c.ValidadePadraoDias).HasDefaultValue(3);
    }
}
