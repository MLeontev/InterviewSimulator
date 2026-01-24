using Microsoft.EntityFrameworkCore;
using Users.Domain;

namespace Users.Infrastructure.Interfaces.DataAccess;

public interface IDbContext
{
    public DbSet<User> Users { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}