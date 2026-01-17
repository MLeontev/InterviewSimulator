using CodeExecution.UseCases.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeExecution.Controllers;

[ApiController]
[Route("api/code-execution/languages")]
public class LanguagesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSupportedLanguages(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLanguagesQuery(), cancellationToken);
        return Ok(result);
    }
}