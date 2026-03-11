using CodeExecution.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Interview.Presentation;

public sealed class CodeSubmissionCompletedConsumer(
    ILogger<CodeSubmissionCompletedConsumer> logger) : IConsumer<CodeSubmissionCompleted>
{
    public Task Consume(ConsumeContext<CodeSubmissionCompleted> context)
    {
        // обработка события
        return Task.CompletedTask;
    }
}