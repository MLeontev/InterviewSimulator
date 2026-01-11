using CodeExecution.Infrastructure.Interfaces.CodeExecution;
using MediatR;

namespace CodeExecution.UseCases;

public record GetLanguagesQuery : IRequest<IReadOnlyCollection<SupportedLanguage>>;

internal class GetLanguagesQueryHandler(ILanguageProvider languageProvider) : IRequestHandler<GetLanguagesQuery, IReadOnlyCollection<SupportedLanguage>>
{
    public Task<IReadOnlyCollection<SupportedLanguage>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SupportedLanguage> result =
            languageProvider.GetSupportedLanguages()
                .ToList()
                .AsReadOnly();

        return Task.FromResult(result);
    }
}