namespace Interview.Domain.Enums;

public enum QuestionStatus
{
    NotStarted,         // Задача ещё не начата
    InProgress,         // Пользователь начал редактировать код
    Skipped,            // Пропущена
    EvaluatingCode,     // Прогон тестов черновика
    EvaluatedCode,      // Результат тестов готов
    Submitted,          // Финальная отправка для AI
    EvaluatingAi,       // AI оценивает финальную попытку
    EvaluatedAi,        // AI завершил оценку
    AiEvaluationFailed  // AI-оценка не удалась после всех ретраев
}