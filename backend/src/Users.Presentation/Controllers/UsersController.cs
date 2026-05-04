using System.Security.Claims;
using Framework.Controllers;
using Framework.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Users.UseCases.Users.Commands;
using Users.UseCases.Users.Queries;

namespace Users.Presentation.Controllers;

/// <summary>
/// Пользователи
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Зарегистрировать пользователя
    /// </summary>
    /// <remarks>
    /// Email должен быть уникальным.
    /// После регистрации пользователь создается в сервисе аутентификации и в базе приложения.
    /// </remarks>
    /// <param name="request">Данные для регистрации пользователя</param>
    /// <response code="201">Зарегистрированный пользователь</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="409">Пользователь с указанным email уже зарегистрирован</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RegisterUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.IsFailure ? result.ToProblem() : StatusCode(StatusCodes.Status201Created, result.Value);
    }
    
    /// <summary>
    /// Получить текущего пользователя
    /// </summary>
    /// <response code="200">Данные текущего пользователя</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="404">Пользователь не найден в базе приложения</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var identityId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityId))
            return Unauthorized();

        var result = await sender.Send(new GetCurrentUserQuery(identityId), cancellationToken);
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
}
