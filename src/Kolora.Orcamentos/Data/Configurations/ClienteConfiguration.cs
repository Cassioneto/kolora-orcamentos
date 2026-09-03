namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        b.Property(c => c.Telefone).HasMaxLength(50);
    }
}
