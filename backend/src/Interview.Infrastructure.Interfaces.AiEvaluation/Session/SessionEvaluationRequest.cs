namespace Interview.Infrastructure.Interfaces.AiEvaluation.Session;

public record SessionEvaluationRequest(
    string PresetName,
    string TechnologyStack,
    IReadOnlyList<SessionQuestionResult> QuestionResults,
    IReadOnlyList<SessionCompetencyResult> CompetencyResults,
    double OverallScore);