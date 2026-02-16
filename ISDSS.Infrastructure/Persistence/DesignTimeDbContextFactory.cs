using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ISDSS.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ISDSS_ConnectionString")
            ?? "Server=localhost;Port=3306;Database=isdss;User Id=root;Password=2570;TreatTinyAsBoolean=true;";

        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(conn, ServerVersion.AutoDetect(conn));

        return new AppDbContext(builder.Options);
    }
}
