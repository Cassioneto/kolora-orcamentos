namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> b)
    {
        b.HasKey(o => o.Id);
        b.Property(o => o.ItensJson).IsRequired();
        b.Property(o => o.Total).HasColumnType("decimal(18,2)");
        b.Property(o => o.CaminhoPdf).HasMaxLength(500);
    }
}
