using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;

namespace Kolora.Orcamentos.Data.Configurations;

public class FilaEnvioWhatsAppConfiguration : IEntityTypeConfiguration<FilaEnvioWhatsApp>
{
    public void Configure(EntityTypeBuilder<FilaEnvioWhatsApp> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.TelefoneDestino).HasMaxLength(30).IsRequired();
        builder.Property(f => f.JidDestino).HasMaxLength(40).IsRequired();
        builder.Property(f => f.PdfPath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.Legenda).HasMaxLength(1000);
        builder.Property(f => f.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Pendente");
        builder.Property(f => f.UltimoErro).HasMaxLength(500);
        builder.HasIndex(f => new { f.GraficaId, f.Status });
    }
}
