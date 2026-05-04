using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Interview.UseCases.InterviewQuestions.Queries;

public record GetCurrentInterviewQuestionQuery(Guid CandidateId) : IRequest<Result<CurrentInterviewQuestion>>;

internal class GetCurrentInterviewQuestionQueryHandler(
    IDbContext dbContext,
    ICurrentSessionResolver currentSessionResolver) : IRequestHandler<GetCurrentInterviewQuestionQuery, Result<CurrentInterviewQuestion>>
{
    public async Task<Result<CurrentInterviewQuestion>> Handle(GetCurrentInterviewQuestionQuery request, CancellationToken ct)
    {
        var sessionId = await currentSessionResolver.GetCurrentSessionIdAsync(request.CandidateId, ct);
        if (sessionId is null)
            return Result.Failure<CurrentInterviewQuestion>(
                Error.NotFound("SESSION_NOT_FOUND", "Текущая сессия интервью не найдена"));
        
        var question = await dbContext.InterviewQuestions
            .AsNoTracking()
            .Where(x => x.InterviewSessionId == sessionId)
            .Where(x => !InterviewQuestionStatusRules.Terminal.Contains(x.Status))
            .OrderBy(x => x.OrderIndex)
            .Select(x => new CurrentInterviewQuestion
            {
                QuestionId = x.Id,
                OrderIndex = x.OrderIndex,
                Type = x.Type,
                Title = x.Title,
                Text = x.Text,
                Status = x.Status,
                Answer = x.Answer,
                ProgrammingLanguageCode = x.ProgrammingLanguageCode,
                TimeLimitMs = x.TimeLimitMs,
                MemoryLimitMb = x.MemoryLimitMb,
                OverallVerdict = x.OverallVerdict,
                ErrorMessage = x.ErrorMessage,
                TestCases = x.TestCases
                    .Where(tc => !tc.IsHidden)
                    .OrderBy(tc => tc.OrderIndex)
                    .Select(tc => new TestCaseDto
                    {
                        OrderIndex = tc.OrderIndex,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ActualOutput = tc.ActualOutput,
                        Verdict = tc.Verdict,
                        ExecutionTimeMs = tc.ExecutionTimeMs,
                        MemoryUsedMb = tc.MemoryUsedMb,
                        ErrorMessage = tc.ErrorMessage,
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (question is null)
            return Result.Failure<CurrentInterviewQuestion>(
                Error.NotFound("QUESTION_NOT_FOUND", "Текущее задание не найдено"));

        return Result.Success(question);
    }
}

/// <summary>
/// Текущее задание активной сессии собеседования
/// </summary>
public record CurrentInterviewQuestion
{
    /// <summary>
    /// Идентификатор задания в рамках сессии
    /// </summary>
    public Guid QuestionId { get; init; }

    /// <summary>
    /// Порядковый номер задания в сессии
    /// </summary>
    public int OrderIndex { get; init; }

    /// <summary>
    /// Тип задания
    /// </summary>
    public QuestionType Type { get; init; }

    /// <summary>
    /// Название задания
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Формулировка задания
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Код языка программирования для задачи на написание кода
    /// </summary>
    public string? ProgrammingLanguageCode { get; init; }

    /// <summary>
    /// Состояние выполнения задания
    /// </summary>
    public QuestionStatus Status { get; init; }

    /// <summary>
    /// Текущий ответ кандидата или последний отправленный код
    /// </summary>
    public string? Answer { get; init; }

    /// <summary>
    /// Ограничение времени выполнения кода в миллисекундах
    /// </summary>
    public int? TimeLimitMs { get; init; }

    /// <summary>
    /// Ограничение памяти выполнения кода в мегабайтах
    /// </summary>
    public int? MemoryLimitMb { get; init; }

    /// <summary>
    /// Общий вердикт последней проверки кода
    /// </summary>
    public Verdict OverallVerdict { get; init; }

    /// <summary>
    /// Сообщение об ошибке проверки кода
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Открытые тест-кейсы задачи
    /// </summary>
    public IReadOnlyList<TestCaseDto> TestCases { get; init; } = [];
}

/// <summary>
/// Открытый тест-кейс задачи на написание кода
/// </summary>
public record TestCaseDto
{
    /// <summary>
    /// Порядковый номер тест-кейса
    /// </summary>
    public int OrderIndex { get; init; }

    /// <summary>
    /// Входные данные тест-кейса
    /// </summary>
    public string Input { get; init; } = string.Empty;

    /// <summary>
    /// Ожидаемый вывод программы
    /// </summary>
    public string ExpectedOutput { get; init; } = string.Empty;

    /// <summary>
    /// Фактический вывод программы после последней проверки
    /// </summary>
    public string? ActualOutput { get; init; }

    /// <summary>
    /// Вердикт выполнения тест-кейса
    /// </summary>
    public Verdict Verdict { get; init; }

    /// <summary>
    /// Время выполнения тест-кейса в миллисекундах
    /// </summary>
    public double? ExecutionTimeMs { get; init; }

    /// <summary>
    /// Использованная память в мегабайтах
    /// </summary>
    public double? MemoryUsedMb { get; init; }

    /// <summary>
    /// Сообщение об ошибке выполнения тест-кейса
    /// </summary>
    public string? ErrorMessage { get; init; }
}
