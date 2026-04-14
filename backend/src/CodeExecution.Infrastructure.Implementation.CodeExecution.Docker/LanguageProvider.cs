namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class LanguageProvider(RuntimeConfig runtimeConfig) : IExecutorLanguageProvider
{
    public bool TryGetLanguage(string code, out LanguageInfo? language) => 
        runtimeConfig.TryGetActiveByCode(code, out language);
}