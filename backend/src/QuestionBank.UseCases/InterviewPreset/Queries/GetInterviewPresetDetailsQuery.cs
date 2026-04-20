using Framework.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.UseCases.InterviewPreset.Queries;

public record PresetCompetencyDto(
    Guid CompetencyId,
    string CompetencyName,
    double Weight);

public record InterviewPresetDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<PresetCompetencyDto> Competencies);

public record GetInterviewPresetDetailsQuery(Guid PresetId) : IRequest<Result<InterviewPresetDetailsDto>>;

internal class GetInterviewPresetDetailsQueryHandler(IDbContext dbContext) 
    : IRequestHandler<GetInterviewPresetDetailsQuery, Result<InterviewPresetDetailsDto>>
{
    public async Task<Result<InterviewPresetDetailsDto>> Handle(
        GetInterviewPresetDetailsQuery request, 
        CancellationToken cancellationToken)
    {
        var preset = await dbContext.InterviewPresets
            .AsNoTracking()
            .Where(p => p.Id == request.PresetId)
            .Include(p => p.Technologies)
            .ThenInclude(pt => pt.Technology)
            .Include(p => p.InterviewPresetCompetencies)
            .ThenInclude(pc => pc.Competency)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (preset is null)
            return Result.Failure<InterviewPresetDetailsDto>(
                Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));
        
        var technologies = preset.Technologies
            .Select(x => x.Technology.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        
        var competencies = preset.InterviewPresetCompetencies
            .Select(x => new PresetCompetencyDto(
                x.CompetencyId,
                x.Competency.Name,
                x.Weight))
            .OrderByDescending(x => x.Weight)
            .ToList();
        
        return new InterviewPresetDetailsDto(
            preset.Id,
            preset.Name,
            technologies,
            competencies);
    }
}