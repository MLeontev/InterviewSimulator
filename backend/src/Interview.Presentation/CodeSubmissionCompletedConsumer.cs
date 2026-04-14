using CodeExecution.IntegrationEvents;
using Interview.UseCases.Commands;
using MassTransit;
using MediatR;
using Verdict = Interview.Domain.Enums.Verdict;

namespace Interview.Presentation;

public sealed class CodeSubmissionCompletedConsumer(ISender sender) : IConsumer<CodeSubmissionCompleted>
{
    public async Task Consume(ConsumeContext<CodeSubmissionCompleted> context)
    {
        var message = context.Message;

        var command = new ApplyCodeSubmissionCompletedCommand(
            SubmissionId: message.SubmissionId,
            InterviewQuestionId: message.InterviewQuestionId,
            TestCaseResults: message.TestCaseResults
                .Select(tc => new ApplyCodeSubmissionTestCaseResultDto(
                    InterviewTestCaseId: tc.InterviewTestCaseId,
                    ActualOutput: tc.ActualOutput,
                    Error: tc.Error,
                    TimeElapsedMs: tc.TimeElapsedMs,
                    MemoryUsedMb: tc.MemoryUsedMb,
                    Verdict: MapVerdict(tc.Verdict)))
                .ToList(),
            OverallVerdict: MapVerdict(message.OverallVerdict),
            ErrorMessage: message.ErrorMessage);

        await sender.Send(command, context.CancellationToken);
    }
    
    private static Verdict MapVerdict(CodeExecution.IntegrationEvents.Verdict verdict) =>
        verdict switch
        {
            CodeExecution.IntegrationEvents.Verdict.OK => Verdict.OK,
            CodeExecution.IntegrationEvents.Verdict.CE => Verdict.CE,
            CodeExecution.IntegrationEvents.Verdict.RE => Verdict.RE,
            CodeExecution.IntegrationEvents.Verdict.TLE => Verdict.TLE,
            CodeExecution.IntegrationEvents.Verdict.MLE => Verdict.MLE,
            CodeExecution.IntegrationEvents.Verdict.WA => Verdict.WA,
            _ => Verdict.FailedSystem
        };
}
