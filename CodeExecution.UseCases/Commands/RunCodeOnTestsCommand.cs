using CodeExecution.Domain.Entities;
using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using MediatR;

namespace CodeExecution.UseCases.Commands;

public record RunCodeOnTestsCommand(
    string Code,
    string Language,
    IReadOnlyList<RunCodeOnTestsCaseDto> TestCases,
    int? MaxTimeSeconds = null,
    int? MaxMemoryMb = null) : IRequest<RunCodeOnTestsResultDto>;

public record RunCodeOnTestsCaseDto(
    string Input,
    string ExpectedOutput,
    int Order);

public record RunCodeOnTestsResultDto(
    Verdict OverallVerdict,
    int PassedCount,
    int TotalCount,
    IReadOnlyList<RunCodeOnTestsCaseResultDto> TestCases);

public record RunCodeOnTestsCaseResultDto(
    int Order,
    Verdict Verdict,
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    string Error,
    int ExitCode,
    double TimeElapsedMs,
    double MemoryUsageMb);

internal sealed class RunCodeOnTestsCommandHandler(ICodeExecutor codeExecutor)
    : IRequestHandler<RunCodeOnTestsCommand, RunCodeOnTestsResultDto>
{
    public async Task<RunCodeOnTestsResultDto> Handle(RunCodeOnTestsCommand request, CancellationToken cancellationToken)
    {
        if (request.TestCases.Count == 0)
        {
            return new RunCodeOnTestsResultDto(
                Verdict.None,
                0,
                0,
                []);
        }

        var orderedCases = request.TestCases.OrderBy(x => x.Order).ToList();
        var results = new List<RunCodeOnTestsCaseResultDto>(orderedCases.Count);
        var overallVerdict = Verdict.OK;
        var passedCount = 0;

        foreach (var testCase in orderedCases)
        {
            CodeExecutionResult executionResult;
            try
            {
                executionResult = await codeExecutor.ExecuteCode(
                    request.Code,
                    testCase.Input,
                    request.Language,
                    cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return new RunCodeOnTestsResultDto(
                    Verdict.None,
                    0,
                    orderedCases.Count,
                    []);
            }

            var actualOutput = executionResult.Output.Trim();
            var expectedOutput = testCase.ExpectedOutput.Trim();
            var verdict = DetermineVerdict(
                executionResult,
                actualOutput,
                expectedOutput,
                request.MaxTimeSeconds,
                request.MaxMemoryMb);

            if (verdict == Verdict.OK)
            {
                passedCount++;
            }
            else if (overallVerdict == Verdict.OK)
            {
                overallVerdict = verdict;
            }

            results.Add(new RunCodeOnTestsCaseResultDto(
                testCase.Order,
                verdict,
                testCase.Input,
                testCase.ExpectedOutput,
                actualOutput,
                executionResult.Error.Trim(),
                executionResult.ExitCode,
                executionResult.TimeElapsedMs,
                executionResult.MemoryUsageMb));
        }

        return new RunCodeOnTestsResultDto(
            overallVerdict,
            passedCount,
            orderedCases.Count,
            results);
    }

    private static Verdict DetermineVerdict(
        CodeExecutionResult executionResult,
        string actualOutput,
        string expectedOutput,
        int? maxTimeSeconds,
        int? maxMemoryMb)
    {
        if (executionResult.Stage == ExecutionStage.Compilation && executionResult.ExitCode != 0)
            return Verdict.CE;

        if (executionResult.Stage == ExecutionStage.Runtime)
        {
            if ((maxTimeSeconds.HasValue
                 && executionResult.TimeElapsedMs > maxTimeSeconds.Value * 1000)
                || executionResult.ExitCode == -1)
                return Verdict.TLE;

            if (maxMemoryMb.HasValue && executionResult.MemoryUsageMb > maxMemoryMb.Value)
                return Verdict.MLE;

            if (executionResult.ExitCode != 0)
                return Verdict.RE;

            if (actualOutput != expectedOutput)
                return Verdict.WA;

            return Verdict.OK;
        }

        return Verdict.None;
    }
}
