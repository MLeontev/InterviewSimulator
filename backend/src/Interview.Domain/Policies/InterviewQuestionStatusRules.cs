using Interview.Domain.Enums;

namespace Interview.Domain.Policies;

public static class InterviewQuestionStatusRules
{
    public static readonly HashSet<QuestionStatus> Terminal =
    [
        QuestionStatus.Skipped,
        QuestionStatus.Submitted,
        QuestionStatus.EvaluatingAi,
        QuestionStatus.EvaluatedAi,
        QuestionStatus.AiEvaluationFailed
    ];

    public static readonly HashSet<QuestionStatus> Active =
    [
        QuestionStatus.NotStarted,
        QuestionStatus.InProgress,
        QuestionStatus.EvaluatingCode,
        QuestionStatus.EvaluatedCode
    ];

    public static readonly HashSet<QuestionStatus> PendingSessionEvaluation =
    [
        QuestionStatus.Submitted,
        QuestionStatus.EvaluatingCode,
        QuestionStatus.EvaluatedCode,
        QuestionStatus.EvaluatingAi
    ];

    public static readonly HashSet<QuestionStatus> Answered =
    [
        QuestionStatus.Submitted,
        QuestionStatus.EvaluatingAi,
        QuestionStatus.EvaluatedAi,
        QuestionStatus.AiEvaluationFailed
    ];
}
