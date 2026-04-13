namespace InterviewSimulator.API.Extensions;

internal static class ConfigurationExtensions
{
    internal static WebApplicationBuilder AddAppConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile(
            "runtimes.json", 
            optional: false, 
            reloadOnChange: true);

        return builder;
    }
}
