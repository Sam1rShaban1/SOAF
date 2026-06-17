using Microsoft.EntityFrameworkCore;
using MyApi.Models;

// Entity Framework Core database context for the application.
// Configures the Product entity mapping and relationships with the database schema.
namespace MyApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet representing the Products table in the database.
    public DbSet<Product> Products => Set<Product>();

    // Fluent API configuration for the Product entity.
    // Defines primary key, required field constraints, and column types.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);                            // Primary key.
            e.Property(p => p.Name)
                .HasMaxLength(200)                           // String length constraint.
                .IsRequired();                               // Name is mandatory.
            e.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");                  // Precision for monetary values.
        });
    }
}
