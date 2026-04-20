using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeExecution.UseCases.CodeSubmissions.Commands;

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
        
        var submission = CodeSubmission.Create(
            request.SubmissionId,
            request.InterviewQuestionId,
            request.Code,
            request.LanguageCode,
            DateTime.UtcNow,
            request.TimeLimitMs,
            request.MemoryLimitMb,
            request.TestCases
                .OrderBy(x => x.OrderIndex)
                .Select(x => CodeSubmissionTestCase.Create(
                    x.InterviewTestCaseId,
                    x.OrderIndex,
                    x.Input,
                    x.ExpectedOutput))
                .ToList());

        await dbContext.CodeSubmissions.AddAsync(submission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
