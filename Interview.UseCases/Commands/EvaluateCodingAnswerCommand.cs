using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record EvaluateCodingAnswerCommand(Guid QuestionId) :  IRequest<Result>;

internal class EvaluateCodingAnswerCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService) : IRequestHandler<EvaluateCodingAnswerCommand, Result>
{
    public async Task<Result> Handle(EvaluateCodingAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await dbContext.InterviewQuestions
            .Include(x => x.TestCases)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);
        
        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Coding)
            return Result.Failure(Error.Business("QUESTION_NOT_CODING", "Задание не является алгоритмической задачей"));

        if (question.Status != QuestionStatus.EvaluatingAi)
            return Result.Failure(Error.Business("QUESTION_NOT_IN_EVALUATING_AI", "Задание не готово к AI-оценке"));

        try
        {
            var passedCount = question.TestCases.Count(x => x.Verdict == Verdict.OK);
            var failedTest = question.TestCases
                .OrderBy(x => x.OrderIndex)
                .FirstOrDefault(x => x.Verdict is not (Verdict.OK or Verdict.None or Verdict.FailedSystem));

            var aiResult = await aiEvaluationService.EvaluateCodingAsync(
                new CodingEvaluationRequest(
                    QuestionText: question.Text,
                    ReferenceSolution: question.ReferenceSolution,
                    CandidateCode: question.Answer ?? string.Empty,
                    OverallVerdict: question.OverallVerdict.ToString(),
                    PassedCount: passedCount,
                    TotalTests: question.TestCases.Count,
                    FirstFailedTest: failedTest is not null ? new CodingFailedTestCase(
                        failedTest.Input,
                        failedTest.ExpectedOutput,
                        failedTest.ActualOutput,
                        failedTest.Verdict.ToString(),
                        failedTest.ErrorMessage) : null),
                cancellationToken);

            question.AiFeedbackJson = aiResult.RawJson;
            question.EvaluatedAt = DateTime.UtcNow;
            question.QuestionVerdict = MapVerdict(aiResult.Score);
            question.Status = QuestionStatus.EvaluatedAi;
            question.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            question.Status = QuestionStatus.EvaluatedAi;
            question.ErrorMessage = $"Ошибка AI-оценки кода: {ex.Message}";
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(Error.External("AI_EVALUATION_FAILED", "Не удалось выполнить AI-оценку"));
        }
    }
    
    private static QuestionVerdict MapVerdict(int score) =>
        score switch
        {
            <= 3 => QuestionVerdict.Incorrect,
            <= 6 => QuestionVerdict.PartiallyCorrect,
            _ => QuestionVerdict.Correct
        };
}