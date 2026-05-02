namespace Interview.Presentation.Requests;

public record PatchCurrentInterviewQuestionRequest(InterviewQuestionStatusPatch Status);

public enum InterviewQuestionStatusPatch
{
    InProgress,
    Skipped
}