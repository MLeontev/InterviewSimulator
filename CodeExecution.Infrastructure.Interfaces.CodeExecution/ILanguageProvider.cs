namespace CodeExecution.Infrastructure.Interfaces.CodeExecution;

public interface ILanguageProvider
{
    IEnumerable<SupportedLanguage> GetSupportedLanguages();
    bool IsSupported(string code);
}