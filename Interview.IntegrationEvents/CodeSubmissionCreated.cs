namespace Interview.IntegrationEvents;

public record CodeSubmissionCreated(
    Guid SubmissionId,
    string Code,
    string Language,
    TestCaseDto[] TestCases,
    int? MaxTimeSeconds = null,
    int? MaxMemoryMb = null);
    
public record TestCaseDto(
    int Order,
    string Input,
    string ExpectedOutput);