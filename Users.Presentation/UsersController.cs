using Framework.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Users.UseCases.Commands;

namespace Users.Presentation;

[ApiController]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}