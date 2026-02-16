using Microsoft.EntityFrameworkCore;
using ISDSS.Domain.Entities;
using System.Text.Json;
using ISDSS.Infrastructure.Security;

namespace ISDSS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var conn = File.Exists(path)
                ? JsonDocument.Parse(File.ReadAllText(path))
                    .RootElement.GetProperty("ConnectionStrings")
                    .GetProperty("Default").GetString()
                // дефолтный доступ — отдельный пользователь isdss
                : "Server=localhost;Port=3306;Database=isdss;User Id=root;Password=2570;TreatTinyAsBoolean=true;";

            optionsBuilder.UseMySql(conn!, ServerVersion.AutoDetect(conn), builder =>
            {
                builder.MigrationsHistoryTable("SchemaVersions");
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Students
        modelBuilder.Entity<Student>(e =>
        {
            e.Property(p => p.FullName)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(p => p.Email)
                .HasMaxLength(200);

            // Уникальный индекс на Email, допускаем NULL (уникальность при наличии)
            // Для Pomelo + MySQL 8 фильтр задаём так:
            e.HasIndex(p => p.Email)
                .IsUnique()
                .HasFilter("`Email` IS NOT NULL");

            e.Property(p => p.CompliancePercent)
                .HasPrecision(5, 2);

            // CHECK 0..100 (MySQL 8 поддерживает)
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Students_CompliancePercent", "`CompliancePercent` >= 0 AND `CompliancePercent` <= 100"));
        });

        // Courses
        modelBuilder.Entity<Course>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.HasOne(c => c.AssignedUser)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Assessments (FK + precision)
        modelBuilder.Entity( typeof(Assessment), e =>
        {
            var b = modelBuilder.Entity<Assessment>();
            b.Property(p => p.Score).HasPrecision(5, 2);

            b.HasOne<Student>()
             .WithMany()
             .HasForeignKey(p => p.StudentId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<Course>()
             .WithMany()
             .HasForeignKey(p => p.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.Property(p => p.Login)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(p => p.PasswordHash)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(p => p.RoleTitle)
                .HasMaxLength(100);

            e.Property(p => p.AccessLevel)
                .HasConversion<int>();

            e.HasIndex(p => p.Login)
                .IsUnique();

            e.HasData(new UserAccount
            {
                Id = 1,
                Login = "admin",
                PasswordHash = PasswordHasher.Hash("Admin!123"),
                RoleTitle = "Администратор",
                AccessLevel = UserAccessLevel.Admin
            });
        });
    }
}
