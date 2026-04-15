using FluentAssertions;
using Interview.Domain.Enums;
using Interview.Domain.Policies;

namespace Interview.Domain.Tests.Policies;

public class InterviewSessionScoringPolicyTests
{
    [Fact]
    public void CalculateOverallScore_ShouldReturnZero_WhenThereAreNoScores()
    {
        var score = InterviewSessionScoringPolicy.CalculateOverallScore([]);

        score.Should().Be(0);
    }

    [Fact]
    public void CalculateOverallScore_ShouldReturnAverage()
    {
        var score = InterviewSessionScoringPolicy.CalculateOverallScore([8, 5]);

        score.Should().Be(6.5);
    }

    [Fact]
    public void CalculateOverallScore_ShouldRoundFractionalAverageToTwoDecimals()
    {
        var score = InterviewSessionScoringPolicy.CalculateOverallScore([8, 7, 5]);

        score.Should().Be(6.67);
    }

    [Theory]
    [InlineData(7.0, SessionVerdict.Passed)]
    [InlineData(6.99, SessionVerdict.Borderline)]
    [InlineData(4.0, SessionVerdict.Borderline)]
    [InlineData(3.99, SessionVerdict.Failed)]
    public void ResolveVerdict_ShouldMapScoreToSessionVerdict(
        double overallScore,
        SessionVerdict expectedVerdict)
    {
        var verdict = InterviewSessionScoringPolicy.ResolveVerdict(overallScore);

        verdict.Should().Be(expectedVerdict);
    }
}
