using Framework.Controllers;
using Interview.UseCases.Commands;
using Interview.UseCases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation;

[ApiController]
[Authorize]
[RequireCandidate]
[Route("api/v1/interview-sessions")]
public class InterviewSessionsController(ISender sender) : ControllerBase
{
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
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
}
