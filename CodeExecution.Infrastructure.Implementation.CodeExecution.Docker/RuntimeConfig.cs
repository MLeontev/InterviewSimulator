using Microsoft.Extensions.Configuration;

namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class RuntimeConfig
{
    private readonly Dictionary<string, LanguageInfo> _languages;

    public RuntimeConfig(IConfiguration configuration)
    {
        var runtimes = configuration
            .GetSection("Runtimes")
            .Get<List<LanguageInfo>>() ?? [];

        _languages = runtimes
            .ToDictionary(
                x => x.Code, 
                x => x, 
                StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<LanguageInfo> GetActive() => _languages.Values.Where(x => x.IsActive);
    
    public bool TryGetActiveByCode(string code, out LanguageInfo? language)
    {
        if (_languages.TryGetValue(code, out var lang) && lang.IsActive)
        {
            language = lang;
            return true;
        }

        language = null;
        return false;
    }
}