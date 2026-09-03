namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class ProdutoMaterialConfiguration : IEntityTypeConfiguration<ProdutoMaterial>
{
    public void Configure(EntityTypeBuilder<ProdutoMaterial> b)
    {
        b.HasKey(pm => new { pm.ProdutoId, pm.MaterialId });
        b.HasOne(pm => pm.Produto).WithMany().HasForeignKey(pm => pm.ProdutoId);
        b.HasOne(pm => pm.Material).WithMany().HasForeignKey(pm => pm.MaterialId);
        b.Property(pm => pm.ConsumoPorUnidade).HasColumnType("decimal(18,4)");
    }
}
