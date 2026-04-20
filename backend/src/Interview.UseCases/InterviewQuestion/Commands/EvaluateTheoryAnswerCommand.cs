using Framework.Domain;
using Framework.UseCases.Resilience;
using Interview.Domain.Enums;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Interview.UseCases.InterviewQuestion.Commands;

public record EvaluateTheoryAnswerCommand(Guid QuestionId) :  IRequest<Result>;

internal class EvaluateTheoryQuestionCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService,
    IOptions<InterviewAiRetryOptions> retryOptions) : IRequestHandler<EvaluateTheoryAnswerCommand, Result>
{
    private readonly InterviewAiRetryOptions _retry = retryOptions.Value;
    
    public async Task<Result> Handle(EvaluateTheoryAnswerCommand request, CancellationToken cancellationToken)
    {
        var question = await dbContext.InterviewQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);
        
        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Theory)
            return Result.Failure(Error.Business("QUESTION_NOT_THEORY", "Задание не является теоретическим вопросом"));

        if (question.Status != QuestionStatus.EvaluatingAi)
            return Result.Failure(Error.Business("QUESTION_NOT_IN_EVALUATING_AI", "Задание не готово к AI-оценке"));

        try
        {
            var aiResult = await aiEvaluationService.EvaluateTheoryAsync(
                new TheoryEvaluationRequest(
                    QuestionText: question.Text,
                    ReferenceSolution: question.ReferenceSolution,
                    CandidateAnswer: question.Answer ?? string.Empty),
                cancellationToken);

            var applyResult = question.ApplyAiEvaluationSuccess(
                aiResult.RawJson,
                aiResult.Score,
                DateTime.UtcNow);

            if (applyResult.IsFailure) return applyResult;

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
                var nextRetryAt = RetryBackoff.NextRetryAtUtc(
                    nextRetry,
                    _retry.BaseDelaySeconds,
                    _retry.MaxDelaySeconds,
                    _retry.JitterSeconds);

                var retryResult = question.ScheduleAiEvaluationRetry(
                    nextRetry,
                    nextRetryAt,
                    _retry.MaxRetries);

                if (retryResult.IsFailure) return retryResult;

                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure(Error.External("AI_EVALUATION_RETRY_SCHEDULED", "Запланирован повтор AI-оценки"));
            }

            var failResult = question.MarkAiEvaluationFailed(
                nextRetry,
                DateTime.UtcNow);

            if (failResult.IsFailure) return failResult;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(Error.External("AI_EVALUATION_FAILED", "Не удалось выполнить AI-оценку"));
        }
    }
}
