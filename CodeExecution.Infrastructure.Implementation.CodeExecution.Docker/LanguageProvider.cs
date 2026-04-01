using CodeExecution.Infrastructure.Interfaces.CodeExecution;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class LanguageProvider(RuntimeConfig runtimeConfig) : ILanguageProvider, IExecutorLanguageProvider
{
    public IReadOnlyCollection<SupportedLanguage> GetSupportedLanguages() =>
        runtimeConfig
            .GetActive()
            .Select(x => new SupportedLanguage(x.Code, x.Name, x.Version))
            .ToList();

    public bool TryGetLanguage(string code, out LanguageInfo? language) => 
        runtimeConfig.TryGetActiveByCode(code, out language);
}