using Interview.Domain.Enums;

namespace Interview.Domain.Models;

public record CodeCheckTestCaseResult(
    Guid InterviewTestCaseId,
    string ActualOutput,
    string? ErrorMessage,
    double TimeElapsedMs,
    double MemoryUsedMb,
    Verdict Verdict);