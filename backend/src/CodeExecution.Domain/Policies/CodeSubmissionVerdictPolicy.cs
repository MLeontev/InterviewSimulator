using CodeExecution.Domain.Enums;

namespace CodeExecution.Domain.Policies;

public static class CodeSubmissionVerdictPolicy
{
    public static Verdict Resolve(
        ExecutionStage stage,
        int exitCode,
        double timeElapsedMs,
        double memoryUsageMb,
        string actualOutput,
        string expectedOutput,
        int? maxTimeMs = null,
        int? maxMemoryMb = null)
    {
        if (stage == ExecutionStage.Compilation && exitCode != 0)
            return Verdict.CE;

        if (stage == ExecutionStage.Runtime)
        {
            if ((maxTimeMs.HasValue && timeElapsedMs > maxTimeMs.Value)
                || exitCode == -1)
                return Verdict.TLE;

            if (maxMemoryMb.HasValue && memoryUsageMb > maxMemoryMb.Value)
                return Verdict.MLE;

            if (exitCode != 0)
                return Verdict.RE;

            if (actualOutput != expectedOutput)
                return Verdict.WA;

            return Verdict.OK;
        }

        return Verdict.FailedSystem;
    }
}
