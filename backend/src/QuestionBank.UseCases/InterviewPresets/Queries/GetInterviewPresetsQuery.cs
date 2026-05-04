using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewPresets.Queries;

/// <summary>
/// Краткая информация о пресете собеседования
/// </summary>
public record InterviewPresetListItemDto
{
    /// <summary>
    /// Идентификатор пресета собеседования
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название пресета собеседования
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

public record GetInterviewPresetsQuery : IRequest<IReadOnlyList<InterviewPresetListItemDto>>;

internal class GetInterviewPresetsQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewPresetsQuery, IReadOnlyList<InterviewPresetListItemDto>>
{
    public async Task<IReadOnlyList<InterviewPresetListItemDto>> Handle(GetInterviewPresetsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.InterviewPresets
            .AsNoTracking()
            .Select(x => new InterviewPresetListItemDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }
}
