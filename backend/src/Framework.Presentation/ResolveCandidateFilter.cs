using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Users.ModuleContract;

namespace Framework.Controllers;

public class ResolveCandidateFilter(IUsersApi usersApi) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var identityId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrWhiteSpace(identityId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var candidateId = await usersApi.GetUserIdByIdentityIdAsync(identityId, context.HttpContext.RequestAborted);
        if (candidateId is null)
        {
            context.Result = new NotFoundObjectResult(new
            {
                code = "USER_NOT_FOUND",
                message = "Пользователь не найден. Сначала зарегистрируйтесь."
            });
            return;
        }
        
        context.HttpContext.Items["CandidateId"] = candidateId.Value;
        await next();
    }
}