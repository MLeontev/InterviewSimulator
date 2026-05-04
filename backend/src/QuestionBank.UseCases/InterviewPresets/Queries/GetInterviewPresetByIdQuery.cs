using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewPresets.Queries;

/// <summary>
/// Подробная информация о пресете собеседования
/// </summary>
public record InterviewPresetDto
{
    /// <summary>
    /// Идентификатор пресета собеседования
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название пресета собеседования
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Уровень подготовки, для которого предназначен пресет
    /// </summary>
    public string Grade { get; init; } = string.Empty;

    /// <summary>
    /// Специализация, для которой предназначен пресет
    /// </summary>
    public string Specialization { get; init; } = string.Empty;

    /// <summary>
    /// Технологии, входящие в пресет
    /// </summary>
    public IReadOnlyList<TechnologyDto> Technologies { get; init; } = [];
}

/// <summary>
/// Технология из состава пресета собеседования
/// </summary>
public record TechnologyDto
{
    /// <summary>
    /// Идентификатор технологии
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название технологии
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Категория технологии
    /// </summary>
    public TechnologyCategory Category { get; init; }
}

public record GetInterviewPresetByIdQuery(Guid PresetId) : IRequest<Result<InterviewPresetDto>>;

internal class GetInterviewPresetByIdQueryHandler(IDbContext dbContext) : IRequestHandler<GetInterviewPresetByIdQuery, Result<InterviewPresetDto>>
{
    public async Task<Result<InterviewPresetDto>> Handle(GetInterviewPresetByIdQuery request, CancellationToken cancellationToken)
    {
        var preset = await dbContext.InterviewPresets
            .AsNoTracking()
            .Where(x => x.Id == request.PresetId)
            .Include(x => x.Grade)
            .Include(x => x.Specialization)
            .Include(x => x.Technologies)
            .ThenInclude(t => t.Technology)
            .Select(x => new InterviewPresetDto
            {
                Id = x.Id,
                Name = x.Name,
                Grade = x.Grade.Name,
                Specialization = x.Specialization.Name,
                Technologies = x.Technologies
                    .Select(t => new TechnologyDto
                    {
                        Id = t.Technology.Id,
                        Name = t.Technology.Name,
                        Category = t.Technology.Category
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (preset is null)
        {
            return Result.Failure<InterviewPresetDto>(Error.NotFound(
                "PRESET_NOT_FOUND",
                "Пресет интервью не найден"));
        }

        return preset;
    }
}
