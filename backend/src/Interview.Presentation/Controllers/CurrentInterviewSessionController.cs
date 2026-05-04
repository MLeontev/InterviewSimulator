using Framework.Controllers;
using Framework.Domain;
using Interview.Presentation.Requests;
using Interview.UseCases.InterviewQuestions.Commands;
using Interview.UseCases.InterviewQuestions.Queries;
using Interview.UseCases.InterviewSessions.Commands;
using Interview.UseCases.InterviewSessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Interview.Presentation.Controllers;

/// <summary>
/// Текущая активная сессия собеседования
/// </summary>
[ApiController]
[Authorize]
[RequireCandidate]
[Produces("application/json")]
[Route("api/v1/interview-sessions/current")]
public class CurrentInterviewSessionController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Получить текущую сессию
    /// </summary>
    /// <response code="200">Текущая активная сессия</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущая активная сессия не найдена</response>
    [HttpGet]
    [ProducesResponseType(typeof(CurrentInterviewSession), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSession(CancellationToken cancellationToken)
    {
        var query = new GetCurrentInterviewSessionQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }
    
    /// <summary>
    /// Получить текущее задание
    /// </summary>
    /// <response code="200">Текущее задание</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущая сессия или текущее задание не найдены</response>
    [HttpGet("question")]
    [ProducesResponseType(typeof(CurrentInterviewQuestion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentQuestion(CancellationToken cancellationToken)
    {
        var query = new GetCurrentInterviewQuestionQuery(HttpContext.GetCandidateId());
        var result = await sender.Send(query, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Ok(result.Value);
    }

    /// <summary>
    /// Изменить состояние текущего задания
    /// </summary>
    /// <remarks>
    /// Поддерживаются только статусы InProgress и Skipped.
    /// InProgress запускает выполнение задания.
    /// Skipped пропускает текущее задание.
    /// </remarks>
    /// <param name="request">Новое состояние текущего задания</param>
    /// <response code="204">Состояние текущего задания изменено</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущее задание не найдено</response>
    /// <response code="422">Задание нельзя изменить в текущем состоянии или время сессии истекло</response>
    [HttpPatch("question")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
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
    
    /// <summary>
    /// Создать черновую отправку кода
    /// </summary>
    /// <remarks>
    /// Черновая отправка запускает проверку кода на тестах, но не завершает выполнение задания.
    /// Повторную отправку можно выполнить после получения результата текущей проверки.
    /// </remarks>
    /// <param name="request">Исходный код решения</param>
    /// <response code="202">Черновая отправка принята на проверку</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущее задание не найдено</response>
    /// <response code="422">Задание не является задачей на написание кода, не готово к проверке или время сессии истекло</response>
    [HttpPost("question/draft-code-submissions")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateDraftCodeSubmission([FromBody] SubmitDraftCodeRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitDraftCodeAnswerCommand(HttpContext.GetCandidateId(), request.Code);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    /// <summary>
    /// Создать итоговую отправку кода
    /// </summary>
    /// <remarks>
    /// Итоговую отправку можно выполнить только после успешного завершения тестовой проверки черновика.
    /// После отправки решение передается на AI-оценку.
    /// </remarks>
    /// <response code="202">Итоговая отправка принята на оценку</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущее задание не найдено</response>
    /// <response code="422">Задание не является задачей на написание кода, код еще не проверен или время сессии истекло</response>
    [HttpPost("question/code-submissions")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCodeSubmission(CancellationToken cancellationToken)
    {
        var command = new SubmitCodeAnswerCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    /// <summary>
    /// Создать ответ на теоретический вопрос
    /// </summary>
    /// <remarks>
    /// После отправки ответ передается на AI-оценку.
    /// </remarks>
    /// <param name="request">Текстовый ответ кандидата</param>
    /// <response code="202">Ответ принят на оценку</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущее задание не найдено</response>
    /// <response code="422">Задание не является теоретическим вопросом, не готово к отправке или время сессии истекло</response>
    [HttpPost("question/theory-answers")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTheoryAnswer([FromBody] SubmitTheoryRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitTheoryAnswerCommand(HttpContext.GetCandidateId(), request.Answer);
        var result = await sender.Send(command, cancellationToken);
        
        return result.IsFailure ? result.ToProblem() : Accepted();
    }
    
    /// <summary>
    /// Изменить состояние текущей сессии
    /// </summary>
    /// <remarks>
    /// Поддерживается только статус Finished.
    /// Вызов завершает текущую активную сессию досрочно.
    /// </remarks>
    /// <param name="request">Новое состояние текущей сессии</param>
    /// <response code="204">Текущая сессия завершена</response>
    /// <response code="400">Некорректное тело запроса или ошибка валидации входных данных</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="403">Доступ к ресурсу запрещен</response>
    /// <response code="404">Текущая активная сессия не найдена</response>
    /// <response code="422">Сессию нельзя завершить в текущем состоянии</response>
    [HttpPatch]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PatchCurrentInterviewSession([FromBody] PatchCurrentInterviewSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new FinishInterviewSessionCommand(HttpContext.GetCandidateId());
        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblem() : NoContent();
    }
}
