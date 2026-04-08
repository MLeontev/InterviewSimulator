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
[Route("api/v1/interview-session")]
public class InterviewSessionController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInterviewSessionCommand(HttpContext.GetCandidateId(), request.InterviewPresetId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Created();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCurrentSession(CancellationToken cancellationToken)
    {
        var query = new GetCurrentInterviewSessionQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
    
    [HttpGet("question")]
    public async Task<IActionResult> GetCurrentQuestion(CancellationToken cancellationToken)
    {
        var query = new GetCurrentInterviewQuestionQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
    
    [HttpPost("question/start")]
    public async Task<IActionResult> StartQuestion(CancellationToken cancellationToken)
    {
        var command = new StartCurrentInterviewQuestionCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
    
    [HttpPost("question/submit-draft-code")]
    public async Task<IActionResult> SubmitDraftCodeAnswer([FromBody] SubmitDraftCodeRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitDraftCodeAnswerCommand(HttpContext.GetCandidateId(), request.Code);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("question/submit-code")]
    public async Task<IActionResult> SubmitCodeAnswer(CancellationToken cancellationToken)
    {
        var command = new SubmitCodeAnswerCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("question/submit-theory")]
    public async Task<IActionResult> SubmitTheoryAnswer([FromBody] SubmitTheoryRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitTheoryAnswerCommand(HttpContext.GetCandidateId(), request.Answer);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("question/skip")]
    public async Task<IActionResult> SkipQuestion(CancellationToken cancellationToken)
    {
        var command = new SkipQuestionCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("finish")]
    public async Task<IActionResult> FinishSession(CancellationToken cancellationToken = default)
    {
        var command = new FinishInterviewSessionCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
}