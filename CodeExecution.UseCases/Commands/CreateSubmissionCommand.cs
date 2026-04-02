using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeExecution.UseCases.Commands;

public record CreateSubmissionCommand(
    Guid SubmissionId,
    Guid InterviewQuestionId,
    string Code,
    string LanguageCode,
    IReadOnlyList<CreateSubmissionTestCaseDto> TestCases,
    int? TimeLimitMs = null,
    int? MemoryLimitMb = null) : IRequest;

public record CreateSubmissionTestCaseDto(
    Guid InterviewTestCaseId,
    int OrderIndex,
    string Input,
    string ExpectedOutput);

internal class CreateSubmissionCommandHandler(IDbContext dbContext) : IRequestHandler<CreateSubmissionCommand>
{
    public async Task Handle(CreateSubmissionCommand request, CancellationToken cancellationToken)
    {
        var exist = await dbContext.CodeSubmissions
            .AnyAsync(x => x.Id == request.SubmissionId, cancellationToken);
        
        if (exist)
            return;
        
        var submission = new CodeSubmission
        {
            Id = request.SubmissionId,
            InterviewQuestionId = request.InterviewQuestionId,
            Code = request.Code,
            LanguageCode = request.LanguageCode,
            TimeLimitMs = request.TimeLimitMs ?? 5_000,
            MemoryLimitMb = request.MemoryLimitMb ?? 64,
            Status = ExecutionStatus.Pending,
            OverallVerdict = Verdict.None,
            ErrorMessage = null,
            CreatedAt = DateTime.UtcNow,
            StartedAt = null,
            CompletedAt = null,
            IsEventPublished = false,
            TestCases = request.TestCases
                .OrderBy(x => x.OrderIndex)
                .Select(x => new CodeSubmissionTestCase
                {
                    Id = Guid.NewGuid(),
                    InterviewTestCaseId = x.InterviewTestCaseId,
                    OrderIndex = x.OrderIndex,
                    Input = x.Input,
                    ExpectedOutput = x.ExpectedOutput,
                    ActualOutput = null,
                    Error = null,
                    ExitCode = null,
                    TimeElapsedMs = null,
                    MemoryUsedMb = null,
                    Verdict = Verdict.None
                })
                .ToList()
        };

        await dbContext.CodeSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}