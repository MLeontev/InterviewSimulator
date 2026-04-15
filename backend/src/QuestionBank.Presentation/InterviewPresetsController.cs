using Framework.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestionBank.UseCases.Queries;

namespace QuestionBank.Presentation;

[ApiController]
[Route("api/v1/interview-presets")]
public class InterviewPresetsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var presets = await sender.Send(new GetInterviewPresetsQuery(), cancellationToken);
        return Ok(presets);
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInterviewPresetByIdQuery(id), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}