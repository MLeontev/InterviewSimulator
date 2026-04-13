namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal interface IExecutorLanguageProvider
{
    bool TryGetLanguage(string code, out LanguageInfo? language);
}