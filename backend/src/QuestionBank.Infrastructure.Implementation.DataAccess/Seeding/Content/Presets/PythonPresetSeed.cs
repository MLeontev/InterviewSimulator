using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets;

public class PythonPresetSeed : PresetSeedBase
{
    protected override Guid PresetId => InterviewPresetIds.PythonMiddle;
    protected override string PresetCode => "python-middle";
    protected override string PresetName => "Python-разработчик (Middle)";
    protected override Guid GradeId => GradeIds.Middle;
    protected override Guid SpecializationId => SpecializationIds.AlgorithmsAndDataStructures;

    protected override IReadOnlyCollection<Guid> RequiredTechnologyIds =>
    [
        TechnologyIds.Python
    ];

    protected override IReadOnlyCollection<PresetCompetencyDefinition> RequiredCompetencies =>
    [
        new(CompetencyIds.PythonCore, 0.25),
        new(CompetencyIds.AlgorithmsBasic, 0.20),
        new(CompetencyIds.DataStructures, 0.20),
        new(CompetencyIds.CodingProblemSolving, 0.25),
        new(CompetencyIds.TestingDebugging, 0.10)
    ];
}