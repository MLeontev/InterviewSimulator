using CodeExecution.Domain.Enums;
using CodeExecution.Domain.Policies;
using FluentAssertions;

namespace CodeExecution.Domain.Tests.Policies;

public class CodeSubmissionVerdictPolicyTests
{
    [Fact]
    public void Resolve_ShouldReturnCE_WhenCompilationFailed()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Compilation,
            exitCode: 1,
            timeElapsedMs: 0,
            memoryUsageMb: 0,
            actualOutput: string.Empty,
            expectedOutput: string.Empty);

        verdict.Should().Be(Verdict.CE);
    }

    [Fact]
    public void Resolve_ShouldReturnTLE_WhenRuntimeExceededTimeLimit()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: 0,
            timeElapsedMs: 1001,
            memoryUsageMb: 32,
            actualOutput: "3",
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.TLE);
    }

    [Fact]
    public void Resolve_ShouldReturnTLE_WhenExitCodeIsMinusOne()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: -1,
            timeElapsedMs: 100,
            memoryUsageMb: 32,
            actualOutput: string.Empty,
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.TLE);
    }

    [Fact]
    public void Resolve_ShouldReturnMLE_WhenRuntimeExceededMemoryLimit()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: 0,
            timeElapsedMs: 100,
            memoryUsageMb: 65,
            actualOutput: "3",
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.MLE);
    }

    [Fact]
    public void Resolve_ShouldReturnRE_WhenRuntimeExitCodeIsNonZero()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: 2,
            timeElapsedMs: 100,
            memoryUsageMb: 32,
            actualOutput: string.Empty,
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.RE);
    }

    [Fact]
    public void Resolve_ShouldReturnWA_WhenOutputDoesNotMatch()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: 0,
            timeElapsedMs: 100,
            memoryUsageMb: 32,
            actualOutput: "4",
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.WA);
    }

    [Fact]
    public void Resolve_ShouldReturnOK_WhenRuntimeSucceededAndOutputMatches()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.Runtime,
            exitCode: 0,
            timeElapsedMs: 100,
            memoryUsageMb: 32,
            actualOutput: "3",
            expectedOutput: "3",
            maxTimeMs: 1000,
            maxMemoryMb: 64);

        verdict.Should().Be(Verdict.OK);
    }

    [Fact]
    public void Resolve_ShouldReturnFailedSystem_WhenStageIsNone()
    {
        var verdict = CodeSubmissionVerdictPolicy.Resolve(
            stage: ExecutionStage.None,
            exitCode: 0,
            timeElapsedMs: 0,
            memoryUsageMb: 0,
            actualOutput: string.Empty,
            expectedOutput: string.Empty);

        verdict.Should().Be(Verdict.FailedSystem);
    }
}
