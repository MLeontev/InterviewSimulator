using Microsoft.Extensions.Configuration;


namespace CodeExecution.Infrastructure.Implementation.CodeExecution;

internal class RuntimeConfig
{
    private readonly Dictionary<string, LanguageInfo> _languages;

    public RuntimeConfig(IConfiguration configuration)
    {
        var runtimes = configuration
            .GetSection("Runtimes")
            .Get<List<LanguageInfo>>() ?? new List<LanguageInfo>();

        _languages = runtimes
            .Where(x => x.IsActive)
            .ToDictionary(x => x.Code, x => x);
    }

    public LanguageInfo Get(string code)
    {
        if (!_languages.TryGetValue(code, out var lang))
            throw new KeyNotFoundException($"Unsupported language: {code}");

        return lang;
    }

    public IEnumerable<LanguageInfo> GetAll() => _languages.Values;
}