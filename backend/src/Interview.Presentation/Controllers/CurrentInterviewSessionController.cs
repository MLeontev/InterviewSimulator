using Framework.Controllers;
using Framework.Domain;
using Interview.Presentation.Requests;
using Interview.UseCases.InterviewQuestions.Commands;
using Interview.UseCases.InterviewQuestions.Queries;
using Interview.UseCases.InterviewSessions.Commands;
using Interview.UseCases.InterviewSessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation.Controllers;

[ApiController]
[Authorize]
[RequireCandidate]
[Route("api/v1/interview-sessions/current")]
public class CurrentInterviewSessionController(ISender sender) : ControllerBase
{
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

    [HttpPatch("question")]
    public async Task<IActionResult> PatchCurrentInterviewQuestion([FromBody] PatchCurrentInterviewQuestionRequest request, CancellationToken cancellationToken)
    {
        var candidateId = HttpContext.GetCandidateId();

        var result = request.Status switch
        {
            InterviewQuestionStatusPatch.InProgress => await sender.Send(
                new StartCurrentInterviewQuestionCommand(candidateId), cancellationToken),
            InterviewQuestionStatusPatch.Skipped => await sender.Send(
                new SkipQuestionCommand(candidateId), cancellationToken),
            _ => Result.Failure(Error.Business("INVALID_QUESTION_STATUS", "Неподдерживаемый статус"))
        };
        
        return result.IsFailure ? result.ToProblem() : NoContent();
    }
    
    [HttpPost("question/draft-code-submissions")]
    public async Task<IActionResult> CreateDraftCodeSubmission([FromBody] SubmitDraftCodeRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitDraftCodeAnswerCommand(HttpContext.GetCandidateId(), request.Code);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("question/code-submissions")]
    public async Task<IActionResult> CreateCodeSubmission(CancellationToken cancellationToken)
    {
        var command = new SubmitCodeAnswerCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPost("question/theory-answers")]
    public async Task<IActionResult> CreateTheoryAnswer([FromBody] SubmitTheoryRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitTheoryAnswerCommand(HttpContext.GetCandidateId(), request.Answer);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    [HttpPatch]
    public async Task<IActionResult> PatchCurrentInterviewSession([FromBody] PatchCurrentInterviewSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new FinishInterviewSessionCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
}
