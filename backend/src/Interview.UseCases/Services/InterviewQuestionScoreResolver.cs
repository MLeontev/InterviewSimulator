using Interview.Domain;
using Interview.Domain.Entities;
using Interview.Domain.Policies;

namespace Interview.UseCases.Services;

internal static class InterviewQuestionScoreResolver
{
    public static int Resolve(InterviewQuestion q)
    {
        if (AiFeedbackJsonParser.TryParseQuestion(q.AiFeedbackJson, out var aiScore, out _))
            return aiScore;

        return InterviewQuestionScoringPolicy.Resolve(q);
    }
}
