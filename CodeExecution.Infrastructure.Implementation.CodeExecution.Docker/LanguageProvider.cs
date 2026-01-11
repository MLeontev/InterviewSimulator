using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class LanguageProvider(RuntimeConfig runtimeConfig) : ILanguageProvider, IExecutorLanguageProvider
{
    public IEnumerable<SupportedLanguage> GetSupportedLanguages()
    {
        return runtimeConfig
            .GetAll()
            .Select(x => new SupportedLanguage(x.Code, x.Name, x.Version));
    }

    public bool IsSupported(string code)
    {
        return runtimeConfig
            .GetAll()
            .Any(x => x.Code == code);
    }

    public LanguageInfo GetLanguage(string code)
    {
        return runtimeConfig.Get(code);
    }
}