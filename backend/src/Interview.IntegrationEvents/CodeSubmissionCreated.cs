namespace Interview.IntegrationEvents;

public record CodeSubmissionCreated(
    Guid SubmissionId,
    Guid InterviewQuestionId,
    string Code,
    string LanguageCode,
    IReadOnlyList<CodeSubmissionCreatedTestCase> TestCases,
    int? TimeLimitMs = null,
    int? MemoryLimitMb = null);

public record CodeSubmissionCreatedTestCase(
    Guid InterviewTestCaseId,
    int OrderIndex,
    string Input,
    string ExpectedOutput);