using Interview.Domain.Entities;
using Interview.Domain.Enums;

namespace Interview.Domain.Policies;

public static class InterviewQuestionScoringPolicy
{
    public static int Resolve(InterviewQuestion question)
    {
        if (question.Status is QuestionStatus.NotStarted or QuestionStatus.Skipped)
            return 0;

        return FromVerdict(question.QuestionVerdict);
    }

    private static int FromVerdict(QuestionVerdict verdict) => verdict switch
    {
        QuestionVerdict.Correct => 8,
        QuestionVerdict.PartiallyCorrect => 5,
        QuestionVerdict.Incorrect => 2,
        _ => 0
    };
}
