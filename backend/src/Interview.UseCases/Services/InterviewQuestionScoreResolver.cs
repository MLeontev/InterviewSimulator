using Interview.Domain.Policies;
using InterviewQuestionEntity = Interview.Domain.Entities.InterviewQuestion;

namespace Interview.UseCases.Services;

internal static class InterviewQuestionScoreResolver
{
    public static int Resolve(InterviewQuestionEntity q)
    {
        if (AiFeedbackJsonParser.TryParseQuestion(q.AiFeedbackJson, out var aiScore, out _))
            return aiScore;

        return InterviewQuestionScoringPolicy.Resolve(q);
    }
}
