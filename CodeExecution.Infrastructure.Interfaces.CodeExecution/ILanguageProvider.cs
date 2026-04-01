namespace CodeExecution.Infrastructure.Interfaces.CodeExecution;

public interface ILanguageProvider
{
    IReadOnlyCollection<SupportedLanguage> GetSupportedLanguages();
}