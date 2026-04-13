namespace Interview.UseCases;

public class InterviewAiRetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelaySeconds { get; set; } = 5;
    public int MaxDelaySeconds { get; set; } = 60;
    public int JitterSeconds { get; set; } = 3;
}