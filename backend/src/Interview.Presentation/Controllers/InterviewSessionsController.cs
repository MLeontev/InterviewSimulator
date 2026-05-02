using Framework.Controllers;
using Interview.Presentation.Requests;
using Interview.UseCases.InterviewSessions.Commands;
using Interview.UseCases.InterviewSessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation.Controllers;

[ApiController]
[Authorize]
[RequireCandidate]
[Route("api/v1/interview-sessions")]
public class InterviewSessionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInterviewSessionCommand(HttpContext.GetCandidateId(), request.InterviewPresetId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Created();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var query = new GetInterviewSessionHistoryQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpGet("{sessionId:guid}/report")]
    public async Task<IActionResult> GetReport(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = new GetInterviewSessionReportQuery(HttpContext.GetCandidateId(), sessionId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpPost("{sessionId:guid}/ai-evaluations")]
    public async Task<IActionResult> CreateAiEvaluation(Guid sessionId, CancellationToken cancellationToken)
    {
        var command = new RetrySessionAiEvaluationCommand(HttpContext.GetCandidateId(), sessionId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Accepted();
    }
}
