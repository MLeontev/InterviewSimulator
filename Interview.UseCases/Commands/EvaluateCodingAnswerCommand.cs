using Framework.Domain;
using Framework.UseCases.Resilience;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Interview.UseCases.Commands;

public record EvaluateCodingAnswerCommand(Guid QuestionId) :  IRequest<Result>;

internal class EvaluateCodingAnswerCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService,
    IOptions<InterviewAiRetryOptions> options) : IRequestHandler<EvaluateCodingAnswerCommand, Result>
{
    private readonly InterviewAiRetryOptions _retry = options.Value;
    
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
            question.AiRetryCount = 0;
            question.AiNextRetryAt = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var nextRetry = question.AiRetryCount + 1;

            if (nextRetry <= _retry.MaxRetries)
            {
                question.AiRetryCount = nextRetry;
                question.AiNextRetryAt = RetryBackoff.NextRetryAtUtc(
                    nextRetry,
                    _retry.BaseDelaySeconds,
                    _retry.MaxDelaySeconds,
                    _retry.JitterSeconds);
                question.Status = QuestionStatus.Submitted;
                question.ErrorMessage = $"AI временно недоступен. Повтор {nextRetry}/{_retry.MaxRetries}.";

                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure(Error.External("AI_EVALUATION_RETRY_SCHEDULED", "Запланирован повтор AI-оценки"));
            }

            question.Status = QuestionStatus.EvaluatedAi;
            question.EvaluatedAt = DateTime.UtcNow;
            question.AiNextRetryAt = null;
            question.ErrorMessage = "AI-оценка недоступна после нескольких попыток.";
            
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
