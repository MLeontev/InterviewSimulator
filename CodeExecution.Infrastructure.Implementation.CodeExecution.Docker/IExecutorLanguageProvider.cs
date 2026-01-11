namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal interface IExecutorLanguageProvider
{
    LanguageInfo GetLanguage(string code);
}