using CodeExecution.UseCases.CodeSubmissions.Commands;
using Interview.IntegrationEvents;
using MassTransit;
using MediatR;

namespace CodeExecution.Controllers;

public sealed class CodeSubmissionCreatedConsumer(ISender sender) : IConsumer<CodeSubmissionCreated>
{
    public async Task Consume(ConsumeContext<CodeSubmissionCreated> context)
    {
        var message = context.Message;

        await sender.Send(
            new CreateSubmissionCommand(
                SubmissionId: message.SubmissionId,
                InterviewQuestionId: message.InterviewQuestionId,
                Code: message.Code,
                LanguageCode: message.LanguageCode,
                TestCases: message.TestCases
                    .Select(tc => new CreateSubmissionTestCaseDto(
                        InterviewTestCaseId: tc.InterviewTestCaseId,
                        OrderIndex: tc.OrderIndex,
                        Input: tc.Input,
                        ExpectedOutput: tc.ExpectedOutput))
                    .ToList(),
                TimeLimitMs: message.TimeLimitMs,
                MemoryLimitMb: message.MemoryLimitMb),
            context.CancellationToken);
    }
}
