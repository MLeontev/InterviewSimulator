using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

internal class FakeAiEvaluationService : IAiEvaluationService
{
    public async Task<TheoryEvaluationResult> EvaluateTheoryAsync(
        TheoryEvaluationRequest request,
        CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return new TheoryEvaluationResult(
            Score: 8,
            Feedback: "Mocked theory feedback",
            RawJson: "{}");
    }

    public async Task<CodingEvaluationResult> EvaluateCodingAsync(
        CodingEvaluationRequest request,
        CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return new CodingEvaluationResult(
            Score: 7,
            Feedback: "Mocked coding feedback",
            RawJson: "{}");
    }

    public async Task<SessionEvaluationResult> EvaluateSessionAsync(
        SessionEvaluationRequest request,
        CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return new SessionEvaluationResult(
            Summary: "Mocked session summary",
            Strengths: ["Good understanding"],
            Weaknesses: ["Needs improvement"],
            Recommendations: ["Practice more"],
            RawJson: "{}");
    }
}