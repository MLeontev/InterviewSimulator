using Interview.Domain;

namespace Interview.UseCases.Services;

internal static class InterviewQuestionScoreResolver
{
    public static int Resolve(InterviewQuestion q)
    {
        if (AiFeedbackJsonParser.TryParseQuestion(q.AiFeedbackJson, out var aiScore, out _))
            return aiScore;

        if (q.Status is QuestionStatus.NotStarted or QuestionStatus.Skipped)
            return 0;

        return FromVerdict(q.QuestionVerdict);
    }

    public static int FromVerdict(QuestionVerdict verdict) => verdict switch
    {
        QuestionVerdict.Correct => 8,
        QuestionVerdict.PartiallyCorrect => 5,
        QuestionVerdict.Incorrect => 2,
        _ => 0
    };
}