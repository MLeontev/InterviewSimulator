using System.Security.Claims;
using Framework.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.UseCases.Users.Commands;
using Users.UseCases.Users.Queries;

namespace Users.Presentation.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
    
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var identityId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityId))
            return Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(identityId), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
