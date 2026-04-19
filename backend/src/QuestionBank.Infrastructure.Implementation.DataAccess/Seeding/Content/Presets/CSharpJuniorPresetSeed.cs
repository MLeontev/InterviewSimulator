using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets;

public class CSharpJuniorPresetSeed : PresetSeedBase
{
    protected override Guid PresetId => InterviewPresetIds.CSharpJunior;
    protected override string PresetCode => "backend-dotnet-junior";
    protected override string PresetName => ".NET backend-разработчик (Junior)";
    protected override Guid GradeId => GradeIds.Junior;
    protected override Guid SpecializationId => SpecializationIds.Backend;

    protected override IReadOnlyCollection<Guid> RequiredTechnologyIds =>
    [
        TechnologyIds.CSharp,
        TechnologyIds.AspNetCore,
        TechnologyIds.EfCore
    ];

    protected override IReadOnlyCollection<PresetCompetencyDefinition> RequiredCompetencies =>
    [
        new(CompetencyIds.CSharpCore, 0.20),
        new(CompetencyIds.AspNetCoreBasics, 0.25),
        new(CompetencyIds.EfCoreBasics, 0.20),
        new(CompetencyIds.DataStructures, 0.10),
        new(CompetencyIds.CodingProblemSolving, 0.15),
        new(CompetencyIds.TestingDebugging, 0.10)
    ];
}
