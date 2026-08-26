using Microsoft.EntityFrameworkCore;
using VisionCheckAI.Server.Data.Entities;

namespace VisionCheckAI.Server.Data;

public class VisionCheckDbContext : DbContext
{
    public VisionCheckDbContext(DbContextOptions<VisionCheckDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<InspectionEntity> Inspections => Set<InspectionEntity>();
    public DbSet<DefectEntity> Defects => Set<DefectEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Default Users
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                Id = "usr-admin",
                Username = "admin",
                PasswordHash = "admin123",
                DisplayName = "Admin User",
                Role = "Administrator"
            },
            new UserEntity
            {
                Id = "usr-supervisor",
                Username = "supervisor",
                PasswordHash = "super123",
                DisplayName = "Sarah Connor",
                Role = "Supervisor"
            },
            new UserEntity
            {
                Id = "usr-inspector",
                Username = "operator",
                PasswordHash = "op123",
                DisplayName = "Alex Rivers",
                Role = "Inspector"
            }
        );

        // Seed Default Products
        modelBuilder.Entity<ProductEntity>().HasData(
            new ProductEntity
            {
                Id = "prod-m8",
                Code = "NUT-M8",
                Name = "M8 Hex Steel Nut",
                Category = "Standard Hex",
                IsActive = true
            },
            new ProductEntity
            {
                Id = "prod-m10",
                Code = "NUT-M10",
                Name = "M10 Flange Nut",
                Category = "Flange Nuts",
                IsActive = true
            },
            new ProductEntity
            {
                Id = "prod-m12",
                Code = "NUT-M12",
                Name = "M12 Nylon Lock Nut",
                Category = "Lock Nuts",
                IsActive = true
            }
        );
    }
}
