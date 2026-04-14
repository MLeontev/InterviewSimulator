using Interview.Domain.Enums;

namespace Interview.Domain.Entities;

public class TestCase
{
    public Guid Id { get; private set; }

    public Guid InterviewQuestionId { get; private set; }
    public InterviewQuestion InterviewQuestion { get; private set; } = null!;

    public int OrderIndex { get; private set; }
    
    public string Input { get; private set; } = string.Empty;
    public string ExpectedOutput { get; private set; } = string.Empty;
    public bool IsHidden { get; private set; }

    public string? ActualOutput { get; private set; }
    public double? ExecutionTimeMs { get; private set; }
    public double? MemoryUsedMb { get; private set; }
    public Verdict Verdict { get; private set; } = Verdict.None;
    public string? ErrorMessage { get; private set; }

    public static TestCase Create(
        string input,
        string expectedOutput,
        bool isHidden,
        int orderIndex)
    {
        return new TestCase
        {
            Id = Guid.NewGuid(),
            Input = input,
            ExpectedOutput = expectedOutput,
            IsHidden = isHidden,
            OrderIndex = orderIndex,
            ActualOutput = null,
            ExecutionTimeMs = null,
            MemoryUsedMb = null,
            Verdict = Verdict.None,
            ErrorMessage = null
        };
    }

    internal void AttachToQuestion(Guid interviewQuestionId)
    {
        InterviewQuestionId = interviewQuestionId;
    }

    public void Reset()
    {
        ActualOutput = null;
        ExecutionTimeMs = null;
        MemoryUsedMb = null;
        Verdict = Verdict.None;
        ErrorMessage = null;
    }
    
    public void ApplyExecutionResult(
        string actualOutput,
        double timeElapsedMs,
        double memoryUsedMb,
        Verdict verdict,
        string? errorMessage)
    {
        ActualOutput = actualOutput;
        ExecutionTimeMs = timeElapsedMs;
        MemoryUsedMb = memoryUsedMb;
        Verdict = verdict;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage;
    }
}
