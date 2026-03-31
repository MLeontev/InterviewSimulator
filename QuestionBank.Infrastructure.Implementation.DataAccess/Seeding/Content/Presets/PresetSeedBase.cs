using Microsoft.EntityFrameworkCore;
using QuestionBank.Domain;
using QuestionBank.Infrastructure.Interfaces.DataAccess;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets;

public abstract class PresetSeedBase : ISeed
{
    public int SeedOrder => SeedOrders.Presets;
    
    protected abstract Guid PresetId { get; }
    protected abstract string PresetCode { get; }
    protected abstract string PresetName { get; }
    protected abstract Guid GradeId { get; }
    protected abstract Guid SpecializationId { get; }
    protected abstract IReadOnlyCollection<Guid> RequiredTechnologyIds { get; }
    protected abstract IReadOnlyCollection<PresetCompetencyDefinition> RequiredCompetencies { get; }
    
    public async Task SeedAsync(IDbContext dbContext, CancellationToken ct = default)
    {
        var preset = await dbContext.InterviewPresets
            .Include(x => x.Technologies)
            .Include(x => x.InterviewPresetCompetencies)
            .FirstOrDefaultAsync(
                x => x.Id == PresetId || x.Code == PresetCode,
                ct);

        if (preset is null)
        {
            preset = new InterviewPreset
            {
                Id = PresetId,
                Code = PresetCode,
                Name = PresetName,
                GradeId = GradeId,
                SpecializationId = SpecializationId
            };

            dbContext.InterviewPresets.Add(preset);
        }
        else
        {
            preset.Code = PresetCode;
            preset.Name = PresetName;
            preset.GradeId = GradeId;
            preset.SpecializationId = SpecializationId;
        }
        
        SyncTechnologies(dbContext, preset, RequiredTechnologyIds);
        SyncCompetencies(dbContext, preset, RequiredCompetencies);
    }
    
    private void SyncTechnologies(IDbContext dbContext, InterviewPreset preset, IReadOnlyCollection<Guid> requiredTechnologyIds)
    {
        var requiredSet = requiredTechnologyIds.ToHashSet();
        var existingTechnologies = preset.Technologies.ToList();

        foreach (var existingTechnology in existingTechnologies)
        {
            if (!requiredSet.Contains(existingTechnology.TechnologyId))
            {
                dbContext.InterviewPresetTechnologies.Remove(existingTechnology);
            }
        }
        
        var existingTechnologyIds = existingTechnologies
            .Select(x => x.TechnologyId)
            .ToHashSet();

        foreach (var technologyId in requiredSet)
        {
            if (!existingTechnologyIds.Contains(technologyId))
            {
                dbContext.InterviewPresetTechnologies.Add(new InterviewPresetTechnology
                {
                    InterviewPresetId = preset.Id,
                    TechnologyId = technologyId
                });
            }
        }
    }
    
    private void SyncCompetencies(IDbContext dbContext, InterviewPreset preset, IReadOnlyCollection<PresetCompetencyDefinition> requiredCompetencies)
    {
        var existingCompetencies = preset.InterviewPresetCompetencies.ToList();
        var requiredById = requiredCompetencies.ToDictionary(x => x.CompetencyId);

        foreach (var existingCompetency in existingCompetencies)
        {
            if (!requiredById.ContainsKey(existingCompetency.CompetencyId))
            {
                dbContext.InterviewPresetCompetencies.Remove(existingCompetency);
            }
        }

        foreach (var requiredCompetency in requiredCompetencies)
        {
            var existing = existingCompetencies
                .FirstOrDefault(x => x.CompetencyId == requiredCompetency.CompetencyId);

            if (existing is null)
            {
                dbContext.InterviewPresetCompetencies.Add(new InterviewPresetCompetency
                {
                    InterviewPresetId = preset.Id,
                    CompetencyId = requiredCompetency.CompetencyId,
                    Weight = requiredCompetency.Weight
                });
                
                continue;
            }
            
            existing.Weight = requiredCompetency.Weight;
        }
    }
    
    protected record PresetCompetencyDefinition(Guid CompetencyId, double Weight);
}