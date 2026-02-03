using Framework.Controllers;
using Interview.UseCases.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation;

[ApiController]
[Route("api/v1/interview-questions")]
public class InterviewQuestionsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuestionById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInterviewQuestionQuery(id), ct);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}