using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

namespace Interview.Infrastructure.Interfaces.AiEvaluation;

public interface IAiEvaluationService
{
    Task<TheoryEvaluationResult> EvaluateTheoryAsync(TheoryEvaluationRequest request, CancellationToken ct = default);
    Task<CodingEvaluationResult> EvaluateCodingAsync(CodingEvaluationRequest request, CancellationToken ct = default);
    Task<SessionEvaluationResult> EvaluateSessionAsync(SessionEvaluationRequest request, CancellationToken ct = default);
}