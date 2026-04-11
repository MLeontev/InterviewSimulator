namespace Framework.UseCases.Resilience;

public static class RetryBackoff
{
    public static DateTime NextRetryAtUtc(
        int retryNumber,
        int baseDelaySeconds,
        int maxDelaySeconds,
        int jitterSeconds)
    {
        var retry = Math.Max(1, retryNumber);
        var baseDelay = Math.Max(1, baseDelaySeconds);
        var maxDelay = Math.Max(baseDelay, maxDelaySeconds);
        var jitterMax = Math.Max(0, jitterSeconds);

        var exp = Math.Clamp(retry - 1, 0, 10);
        var delay = baseDelay * (int)Math.Pow(2, exp);
        delay = Math.Min(delay, maxDelay);

        var jitter = jitterMax == 0
            ? 0
            : Random.Shared.Next(0, jitterMax + 1);

        return DateTime.UtcNow.AddSeconds(delay + jitter);
    }
}