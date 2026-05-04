using Framework.Controllers;
using Framework.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestionBank.UseCases.InterviewPresets.Queries;

namespace QuestionBank.Presentation.Controllers;

/// <summary>
/// Пресеты собеседований
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/v1/interview-presets")]
public class InterviewPresetsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Получить список пресетов собеседований
    /// </summary>
    /// <response code="200">Список пресетов</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewPresetListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var presets = await sender.Send(new GetInterviewPresetsQuery(), cancellationToken);
        return Ok(presets);
    }

    /// <summary>
    /// Получить пресет собеседования
    /// </summary>
    /// <param name="id">Идентификатор пресета собеседования</param>
    /// <response code="200">Пресет собеседования</response>
    /// <response code="404">Пресет собеседования не найден</response>
    [HttpGet("{id:Guid}")]
    [ProducesResponseType(typeof(InterviewPresetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInterviewPresetByIdQuery(id), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
