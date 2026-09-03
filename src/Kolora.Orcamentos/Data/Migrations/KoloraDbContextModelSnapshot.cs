// Snapshot placeholder - gerado manualmente para Fase 1
// Para regerar com dotnet-ef quando .NET 8 SDK estiver no PATH global:
//   dotnet ef migrations add InitialCreate
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kolora.Orcamentos.Data.Migrations
{
    [DbContext(typeof(KoloraDbContext))]
    partial class KoloraDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");
            // Ver InitialCreate.cs para schema completo
#pragma warning restore 612, 618
        }
    }
}
