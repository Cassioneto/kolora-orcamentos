namespace Kolora.Orcamentos.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kolora.Orcamentos.Models.Entities;
public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> b)
    {
        b.HasKey(s => s.Id);
        b.HasData(new SyncState { Id = 1, VersaoSchemaLocal = 1 });
    }
}
