using System.Collections.Concurrent;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;

namespace Backend.IntegrationTests.Infrastructure.Fakes;

public sealed class FakeAiEvaluationService : IAiEvaluationService
{
    private readonly ConcurrentQueue<Func<TheoryEvaluationResult>> _theoryResults = new();
    private readonly ConcurrentQueue<Func<CodingEvaluationResult>> _codingResults = new();
    private readonly ConcurrentQueue<Func<SessionEvaluationResult>> _sessionResults = new();

    public void Reset()
    {
        while (_theoryResults.TryDequeue(out _))
        {
        }

        while (_codingResults.TryDequeue(out _))
        {
        }

        while (_sessionResults.TryDequeue(out _))
        {
        }
    }

    public void EnqueueTheoryResult(TheoryEvaluationResult result)
    {
        _theoryResults.Enqueue(() => result);
    }

    public void EnqueueTheoryException(Exception exception)
    {
        _theoryResults.Enqueue(() => throw exception);
    }

    public void EnqueueCodingResult(CodingEvaluationResult result)
    {
        _codingResults.Enqueue(() => result);
    }

    public void EnqueueCodingException(Exception exception)
    {
        _codingResults.Enqueue(() => throw exception);
    }

    public void EnqueueSessionResult(SessionEvaluationResult result)
    {
        _sessionResults.Enqueue(() => result);
    }

    public void EnqueueSessionException(Exception exception)
    {
        _sessionResults.Enqueue(() => throw exception);
    }

    public Task<TheoryEvaluationResult> EvaluateTheoryAsync(
        TheoryEvaluationRequest request,
        CancellationToken ct = default)
    {
        if (_theoryResults.TryDequeue(out var factory))
            return Task.FromResult(factory());

        const int score = 8;
        const string feedback = "Mocked theory feedback";

        return Task.FromResult(new TheoryEvaluationResult(
            Score: score,
            Feedback: feedback,
            RawJson: $$"""{"score":{{score}},"feedback":"{{feedback}}"}"""));
    }

    public Task<CodingEvaluationResult> EvaluateCodingAsync(
        CodingEvaluationRequest request,
        CancellationToken ct = default)
    {
        if (_codingResults.TryDequeue(out var factory))
            return Task.FromResult(factory());

        const int score = 7;
        const string feedback = "Mocked coding feedback";

        return Task.FromResult(new CodingEvaluationResult(
            Score: score,
            Feedback: feedback,
            RawJson: $$"""{"score":{{score}},"feedback":"{{feedback}}"}"""));
    }

    public Task<SessionEvaluationResult> EvaluateSessionAsync(
        SessionEvaluationRequest request,
        CancellationToken ct = default)
    {
        if (_sessionResults.TryDequeue(out var factory))
            return Task.FromResult(factory());

        const string summary = "Mocked session summary";
        var strengths = new[] { "Good understanding" };
        var weaknesses = new[] { "Needs improvement" };
        var recommendations = new[] { "Practice more" };

        return Task.FromResult(new SessionEvaluationResult(
            Summary: summary,
            Strengths: strengths,
            Weaknesses: weaknesses,
            Recommendations: recommendations,
            RawJson: """
                     {"summary":"Mocked session summary","strengths":["Good understanding"],"weaknesses":["Needs improvement"],"recommendations":["Practice more"]}
                     """));
    }
}
