using Framework.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Controllers;

public static class ResultExtensions
{
    public static IActionResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful Result to a problem");

        return result.Error.Type switch
        {
            ErrorType.None => throw new InvalidOperationException("Result has no error, cannot convert to problem"),
            ErrorType.Validation => new BadRequestObjectResult(result.Error),
            ErrorType.NotFound => new NotFoundObjectResult(result.Error),
            ErrorType.Conflict => new ConflictObjectResult(result.Error),
            ErrorType.Business => new UnprocessableEntityObjectResult(result.Error),
            ErrorType.External => new ObjectResult(result.Error) { StatusCode = StatusCodes.Status502BadGateway },
            _ => new BadRequestObjectResult(result.Error)
        };
    }
}
