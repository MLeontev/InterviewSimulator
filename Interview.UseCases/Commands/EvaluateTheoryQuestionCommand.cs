using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.Commands;

public record EvaluateTheoryQuestionCommand(Guid QuestionId) :  IRequest<Result>;

internal class EvaluateTheoryQuestionCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService) : IRequestHandler<EvaluateTheoryQuestionCommand, Result>
{
    public async Task<Result> Handle(EvaluateTheoryQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await dbContext.InterviewQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken);
        
        if (question is null)
            return Result.Failure(Error.NotFound("QUESTION_NOT_FOUND", "Задание не найдено"));

        if (question.Type != QuestionType.Theory)
            return Result.Failure(Error.Business("QUESTION_NOT_THEORY", "Задание не является теоретическим"));

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
            question.ErrorMessage = $"Ошибка AI-оценки теории: {ex.Message}";
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
