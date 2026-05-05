using Framework.Controllers;
using Framework.Domain;
using Interview.Presentation.Requests;
using Interview.UseCases.InterviewSessions.Commands;
using Interview.UseCases.InterviewSessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation.Controllers;

/// <summary>
/// Сессии собеседований
/// </summary>
[ApiController]
[Authorize]
[RequireCandidate]
[Produces("application/json")]
[Route("api/v1/interview-sessions")]
public class InterviewSessionsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Создать сессию собеседования
    /// </summary>
    /// <remarks>
    /// У кандидата может быть только одна активная сессия одновременно.
    /// Сессия действительна 1 час с момента создания.
    /// </remarks>
    /// <param name="request">Параметры создаваемой сессии</param>
    /// <response code="201">Сессия успешно создана</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Выбранный пресет собеседования не найден</response>
    /// <response code="409">У кандидата уже есть активная сессия собеседования</response>
    /// <response code="422">Для выбранного пресета не удалось получить вопросы или набор вопросов оказался пустым</response>
    /// <response code="502">Ошибка при получении данных из модуля банка вопросов</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInterviewSessionCommand(HttpContext.GetCandidateId(), request.InterviewPresetId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Created();
    }
    
    /// <summary>
    /// Получить историю сессий собеседований
    /// </summary>
    /// <remarks>
    /// Возвращаются только завершенные или оцененные сессии текущего кандидата.
    /// Активная сессия не включается в историю.
    /// </remarks>
    /// <response code="200">История сессий</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Пользователь не найден в базе приложения</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewSessionHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var query = new GetInterviewSessionHistoryQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    /// <summary>
    /// Получить отчет по сессии
    /// </summary>
    /// <remarks>
    /// Отчет доступен только после завершения AI-оценки сессии.
    /// Кандидат может получить отчет только по собственной сессии.
    /// </remarks>
    /// <param name="sessionId">Идентификатор сессии собеседования</param>
    /// <response code="200">Отчет по сессии</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Сессия не найдена</response>
    /// <response code="422">Сессия еще не оценена</response>
    [HttpGet("{sessionId:guid}/report")]
    [ProducesResponseType(typeof(InterviewSessionReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetReport(Guid sessionId, CancellationToken cancellationToken)
    {
        var query = new GetInterviewSessionReportQuery(HttpContext.GetCandidateId(), sessionId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    /// <summary>
    /// Создать повторную AI-оценку сессии
    /// </summary>
    /// <remarks>
    /// Повторную AI-оценку можно запустить только для сессии, у которой предыдущая AI-оценка завершилась ошибкой.
    /// </remarks>
    /// <param name="sessionId">Идентификатор сессии собеседования</param>
    /// <response code="202">Повторная AI-оценка запущена</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Сессия не найдена</response>
    /// <response code="422">AI-оценка сессии не была завершена с ошибкой или нет заданий с ошибкой AI-оценки</response>
    [HttpPost("{sessionId:guid}/ai-evaluations")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAiEvaluation(Guid sessionId, CancellationToken cancellationToken)
    {
        var command = new RetrySessionAiEvaluationCommand(HttpContext.GetCandidateId(), sessionId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Accepted();
    }
}
