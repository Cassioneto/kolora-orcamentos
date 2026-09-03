namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b)
    {
        b.HasKey(m => m.Id);
        b.Property(m => m.Nome).IsRequired().HasMaxLength(200);
        b.Property(m => m.Unidade).HasMaxLength(20);
        b.Property(m => m.StockAtual).HasColumnType("decimal(18,2)");
        b.Property(m => m.StockMinimo).HasColumnType("decimal(18,2)");
    }
}
