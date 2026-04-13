namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding;

public class QuestionBankSeedRunner(AppDbContext dbContext, IEnumerable<ISeed> seeds)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        foreach (var seed in seeds.OrderBy(x => x.SeedOrder))
        {
            await seed.SeedAsync(dbContext, ct);
        }
        
        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
