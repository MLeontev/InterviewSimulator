using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding;

public interface ISeed
{
    int SeedOrder { get; }
    
    Task SeedAsync(IDbContext dbContext, CancellationToken ct = default);
}