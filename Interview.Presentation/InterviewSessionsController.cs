using Framework.Controllers;
using Interview.UseCases.Commands;
using Interview.UseCases.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation;

[ApiController]
[Route("api/v1/interview-sessions")]
public class InterviewSessionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateInterviewSessionCommand(request.CandidateId, request.InterviewPresetId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblem()
            : Created($"/api/v1/interview-sessions/{result.Value}", new { sessionId = result.Value });
    }
    
    [HttpGet("candidate/{candidateId:Guid}")]
    public async Task<IActionResult> GetCandidateSessions(Guid candidateId, CancellationToken cancellationToken = default)
    {
        var query = new GetCandidateSessionsQuery(candidateId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
    
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetInterviewSessionByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    [HttpDelete("{id:Guid}")]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteInterviewSessionCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
    
    [HttpPost("{sessionId:guid}/finish")]
    public async Task<IActionResult> FinishSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var command = new FinishInterviewSessionCommand(sessionId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
}
