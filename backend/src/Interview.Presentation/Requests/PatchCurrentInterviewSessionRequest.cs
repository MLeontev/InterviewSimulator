namespace Interview.Presentation.Requests;

public record PatchCurrentInterviewSessionRequest(InterviewSessionStatusPatch Status);

public enum InterviewSessionStatusPatch
{
    Finished
}