namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class PedidoOrcamentoConfiguration : IEntityTypeConfiguration<PedidoOrcamento>
{
    public void Configure(EntityTypeBuilder<PedidoOrcamento> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.ClienteNome).HasMaxLength(200);
        b.Property(p => p.ClienteTelefone).HasMaxLength(50);
        b.Property(p => p.TextoOriginalCliente).HasMaxLength(2000);
        b.Property(p => p.ProdutoDesejado).HasMaxLength(200);
        b.Property(p => p.DescricaoPedido).HasMaxLength(2000);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Quantidade).HasColumnType("decimal(18,2)");
    }
}
