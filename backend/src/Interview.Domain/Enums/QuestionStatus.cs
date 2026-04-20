namespace Interview.Domain.Enums;

public enum QuestionStatus
{
    NotStarted,         // Задача ещё не начата
    InProgress,         // Пользователь начал выполнение
    Skipped,            // Пропущена
    EvaluatingCode,     // Проверка выполнения черновика кода
    EvaluatedCode,      // Результат тестов готов
    Submitted,          // Отправлен финальный ответ
    EvaluatingAi,       // AI оценивает финальную попытку
    EvaluatedAi,        // AI завершил оценку
    AiEvaluationFailed  // AI-оценка не удалась после всех ретраев
}