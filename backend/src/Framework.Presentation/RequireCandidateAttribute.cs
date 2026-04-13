using Microsoft.AspNetCore.Mvc;

namespace Framework.Controllers;

public class RequireCandidateAttribute() : TypeFilterAttribute(typeof(ResolveCandidateFilter));