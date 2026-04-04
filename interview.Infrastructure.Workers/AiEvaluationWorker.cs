using Interview.Domain;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace interview.Infrastructure.Workers;

internal class AiEvaluationWorker(
    IServiceProvider serviceProvider,
    ILogger<AiEvaluationWorker> logger) : BackgroundService
{
    private const int DelayMs = 1000;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessAnswerAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(DelayMs, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в AiEvaluationWorker при обработке очереди AI-оценки");
                await Task.Delay(DelayMs, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessAnswerAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        
        var questions = await dbContext.InterviewQuestions
            .FromSqlRaw("""
                        UPDATE "Interview"."InterviewQuestions" q
                        SET "Status" = 'EvaluatingAi'
                        WHERE q."Id" = (
                            SELECT q2."Id"
                            FROM "Interview"."InterviewQuestions" q2
                            WHERE (
                                (q2."Type" = 'Theory' AND q2."Status" = 'Submitted')
                                OR (q2."Type" = 'Coding' AND q2."Status" = 'EvaluatedCode')
                            )
                              AND q2."Answer" IS NOT NULL
                              AND btrim(q2."Answer") <> ''
                            ORDER BY q2."SubmittedAt" NULLS FIRST
                            FOR UPDATE SKIP LOCKED
                            LIMIT 1
                        )
                        RETURNING *
                        """)
            .AsNoTracking()
            .ToListAsync(ct);

        var question = questions.FirstOrDefault();

        if (question == null)
            return false;
        
        switch (question.Type)
        {
            case QuestionType.Theory:
                await sender.Send(new EvaluateTheoryAnswerCommand(question.Id), ct);
                break;
            case QuestionType.Coding:
                await sender.Send(new EvaluateCodingAnswerCommand(question.Id), ct);
                break;
            default:
                throw new InvalidOperationException($"Unexpected question type: {question.Type}");
        }

        return true;
    }
}