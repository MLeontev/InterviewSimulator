using Interview.Domain.Enums;

namespace Interview.Domain.Policies;

public static class InterviewSessionScoringPolicy
{
    public static double CalculateOverallScore(IReadOnlyCollection<int> questionScores)
    {
        if (questionScores.Count == 0)
            return 0;

        return Math.Round(questionScores.Average(), 2);
    }

    public static SessionVerdict ResolveVerdict(double overallScore) =>
        overallScore switch
        {
            >= 7 => SessionVerdict.Passed,
            >= 4 => SessionVerdict.Borderline,
            _ => SessionVerdict.Failed
        };
}
