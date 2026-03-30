using Microsoft.EntityFrameworkCore;
using Users.Domain;
using Users.Infrastructure.Interfaces.DataAccess;

namespace Users.Infrastructure.Implementation.DataAccess;

public class AppDbContext : DbContext, IDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);
        
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.IdentityId).IsUnique();
    }
}