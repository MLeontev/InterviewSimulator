using Microsoft.AspNetCore.Http;

namespace Framework.Controllers;

public static class HttpContextExtensions
{
    public static Guid GetCandidateId(this HttpContext context)
        => (Guid)context.Items["CandidateId"]!;
}