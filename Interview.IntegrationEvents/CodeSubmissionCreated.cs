namespace Interview.IntegrationEvents;

public record CodeSubmissionCreated(
    Guid SubmissionId,
    string Code,
    string Language,
    TestCaseDto[] TestCases,
    int? MaxTimeSeconds = null,
    int? MaxMemoryMb = null);
    
public record TestCaseDto(
    string Input,
    string ExpectedOutput);