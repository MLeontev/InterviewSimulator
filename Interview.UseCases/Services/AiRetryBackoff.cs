namespace Interview.UseCases.Services;

internal static class AiRetryBackoff
{
    public static DateTime NextRetryAtUtc(int retryNumber, InterviewAiRetryOptions options)
    {
        var exp = Math.Clamp(retryNumber - 1, 0, 10);
        var delay = options.BaseDelaySeconds * (int)Math.Pow(2, exp);
        delay = Math.Min(delay, options.MaxDelaySeconds);

        var jitter = options.JitterSeconds > 0
            ? Random.Shared.Next(0, options.JitterSeconds + 1)
            : 0;
        
        return DateTime.UtcNow.AddSeconds(delay + jitter);
    }
}