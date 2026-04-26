using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Presets;

public class CppJuniorPresetSeed : PresetSeedBase
{
    protected override Guid PresetId => InterviewPresetIds.CppJunior;
    protected override string PresetCode => "cpp-junior";
    protected override string PresetName => "C++ разработчик (Junior)";
    protected override Guid GradeId => GradeIds.Junior;
    protected override Guid SpecializationId => SpecializationIds.AlgorithmsAndDataStructures;

    protected override IReadOnlyCollection<Guid> RequiredTechnologyIds =>
    [
        TechnologyIds.Cpp
    ];

    protected override IReadOnlyCollection<PresetCompetencyDefinition> RequiredCompetencies =>
    [
        new(CompetencyIds.CppCore, 0.25),
        new(CompetencyIds.AlgorithmsBasic, 0.20),
        new(CompetencyIds.DataStructures, 0.20),
        new(CompetencyIds.CodingProblemSolving, 0.25),
        new(CompetencyIds.TestingDebugging, 0.10)
    ];
}
