using CodeExecution.IntegrationEvents;
using Interview.UseCases.Commands;
using MassTransit;
using MediatR;

namespace Interview.Presentation;

public sealed class CodeSubmissionCompletedConsumer(ISender sender) : IConsumer<CodeSubmissionCompleted>
{
    public async Task Consume(ConsumeContext<CodeSubmissionCompleted> context)
    {
        await sender.Send(new ApplyCodeSubmissionCompletedCommand(context.Message), context.CancellationToken);
    }
}
