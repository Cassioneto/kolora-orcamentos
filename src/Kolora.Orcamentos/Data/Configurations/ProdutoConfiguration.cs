namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Nome).IsRequired().HasMaxLength(200);
        b.Property(p => p.TipoCalculo).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.PrecoCustoBase).HasColumnType("decimal(18,2)");
        b.Property(p => p.MargemPadrao).HasColumnType("decimal(18,4)");
    }
}
