using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewPreset.Queries;

public record InterviewPresetDto(
    Guid Id,
    string Name,
    string Grade,
    string Specialization,
    IReadOnlyList<TechnologyDto> Technologies);

public record TechnologyDto(
    Guid Id,
    string Name,
    TechnologyCategory Category);

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
            .Select(x => new InterviewPresetDto(x.Id, x.Name, x.Grade.Name, x.Specialization.Name,
                x.Technologies
                    .Select(t => new TechnologyDto(t.Technology.Id, t.Technology.Name, t.Technology.Category))
                    .ToList()))
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