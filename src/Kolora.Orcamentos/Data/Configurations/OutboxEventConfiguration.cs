namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> b)
    {
        b.HasKey(o => o.Id);
        b.Property(o => o.Entidade).IsRequired().HasMaxLength(50);
        b.Property(o => o.TipoOperacao).HasConversion<string>().HasMaxLength(10);
        b.Property(o => o.PayloadJson).IsRequired();
        b.Property(o => o.StatusSync).IsRequired().HasMaxLength(20);
        b.Property(o => o.UltimoErro).HasMaxLength(2000);
        b.HasIndex(o => o.StatusSync);
        b.HasIndex(o => o.CriadoEm);
    }
}
