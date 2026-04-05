using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;

namespace Interview.UseCases.Commands;

public record SkipQuestionCommand(Guid CandidateId) : IRequest<Result>;

internal class SkipQuestionCommandHandler(
    IDbContext dbContext, 
    ICurrentQuestionResolver currentQuestionResolver) : IRequestHandler<SkipQuestionCommand, Result>
{
    public async Task<Result> Handle(SkipQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await currentQuestionResolver.GetCurrentQuestionAsync(request.CandidateId, cancellationToken);

        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Текущее задание не найдено"));

        if (question.Status is not (QuestionStatus.NotStarted or QuestionStatus.InProgress))
            return Result.Failure(Error.Business("QUESTION_CANNOT_BE_SKIPPED", "Это задание сейчас нельзя пропустить"));

        question.Status = QuestionStatus.Skipped;
        question.Answer = null;
        question.QuestionVerdict = QuestionVerdict.None;
        question.OverallVerdict = Verdict.None;
        question.AiFeedbackJson = null;
        question.ErrorMessage = null;
        question.SubmittedAt = null;
        question.EvaluatedAt = null;

        foreach (var testCase in question.TestCases)
        {
            testCase.ActualOutput = null;
            testCase.ExecutionTimeMs = null;
            testCase.MemoryUsedMb = null;
            testCase.Verdict = Verdict.None;
            testCase.ErrorMessage = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}