using CodeExecution.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeExecution.Infrastructure.Interfaces.DataAccess;

public interface IDbContext
{
    DbSet<CodeSubmission> CodeSubmissions { get; set; }
    DbSet<CodeSubmissionTestCase> CodeSubmissionTestCases { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}