namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class GraficaConfiguration : IEntityTypeConfiguration<Grafica>
{
    public void Configure(EntityTypeBuilder<Grafica> b)
    {
        b.HasKey(g => g.Id);
        b.Property(g => g.Nome).IsRequired().HasMaxLength(200);
        b.Property(g => g.Telefone).HasMaxLength(50);
        b.Property(g => g.Localizacao).HasMaxLength(500);
        b.Property(g => g.LogoPath).HasMaxLength(500);
    }
}
