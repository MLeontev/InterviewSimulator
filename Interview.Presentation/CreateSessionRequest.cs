namespace Interview.Presentation;

public record CreateSessionRequest(
    Guid CandidateId,
    Guid InterviewPresetId);
