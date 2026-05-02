namespace InterviewSimulator.API.Swagger;

internal sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

    public bool Enabled { get; set; } = true;

    public string RoutePrefix { get; set; } = "swagger";

    public SwaggerOAuthOptions OAuth { get; set; } = new();
}
