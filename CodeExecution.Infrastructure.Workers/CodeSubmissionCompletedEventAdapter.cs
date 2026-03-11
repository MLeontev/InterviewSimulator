using CodeExecution.Domain.Entities;
using CodeExecution.IntegrationEvents;
using Verdict = CodeExecution.Domain.Entities.Verdict;

namespace CodeExecution.Infrastructure.Workers;

internal interface ICodeSubmissionCompletedEventAdapter
{
    CodeSubmissionCompleted Adapt(CodeSubmission submission);
}

internal sealed class CodeSubmissionCompletedEventAdapter : ICodeSubmissionCompletedEventAdapter
{
    public CodeSubmissionCompleted Adapt(CodeSubmission submission)
    {
        var testCaseResults = new List<TestCaseResultDto>();
        var passedCount = 0;

        foreach (var testCase in submission.TestCases)
        {
            testCaseResults.Add(new TestCaseResultDto(
                TestCaseId: testCase.Id,
                Input: testCase.Input,
                ExpectedOutput: testCase.ExpectedOutput,
                ActualOutput: testCase.ActualOutput,
                Order: testCase.Order,
                Error: testCase.Error,
                ExitCode: testCase.ExitCode,
                TimeElapsed: testCase.TimeElapsed,
                MemoryUsage: testCase.MemoryUsage,
                Verdict: MapVerdict(testCase.Verdict)));

            if (testCase.Verdict == Verdict.OK)
                passedCount++;
            else
                break;
        }

        var overallVerdict = submission.Status == ExecutionStatus.Failed
            ? IntegrationEvents.Verdict.FailedSystem
            : MapVerdict(submission.OverallVerdict);

        return new CodeSubmissionCompleted(
            SubmissionId: submission.Id,
            TestCaseResults: testCaseResults.ToArray(),
            OverallVerdict: overallVerdict,
            PassedCount: passedCount,
            TotalTests: submission.TestCases.Count);
    }

    private static IntegrationEvents.Verdict MapVerdict(Verdict verdict) =>
        verdict switch
        {
            Verdict.OK => IntegrationEvents.Verdict.OK,
            Verdict.CE => IntegrationEvents.Verdict.CE,
            Verdict.RE => IntegrationEvents.Verdict.RE,
            Verdict.TLE => IntegrationEvents.Verdict.TLE,
            Verdict.MLE => IntegrationEvents.Verdict.MLE,
            Verdict.WA => IntegrationEvents.Verdict.WA,
            _ => IntegrationEvents.Verdict.FailedSystem
        };
}
