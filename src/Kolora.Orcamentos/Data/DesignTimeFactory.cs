using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kolora.Orcamentos.Data;

/// <summary>
/// Factory para o dotnet-ef gerar migrations em design-time (dotnet ef migrations add ...).
/// </summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<KoloraDbContext>
{
    public KoloraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KoloraDbContext>()
            .UseSqlite("Data Source=kolora.design.db")
            .Options;
        return new KoloraDbContext(options);
    }
}
