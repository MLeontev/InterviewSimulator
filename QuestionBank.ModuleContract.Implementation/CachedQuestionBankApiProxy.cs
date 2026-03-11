using Microsoft.Extensions.Caching.Memory;
using QuestionBank.InternalApi;

namespace QuestionBank.ModuleContract.Implementation;

internal sealed class CachedQuestionBankApiProxy(
    IQuestionBankApi inner,
    IMemoryCache cache) : IQuestionBankApi
{
    private static readonly TimeSpan PresetTtl = TimeSpan.FromMinutes(10);

    public Task<IReadOnlyList<InterviewQuestionApiDto>> GetQuestionsAsync(Guid interviewPresetId, int totalQuestions)
        => inner.GetQuestionsAsync(interviewPresetId, totalQuestions);

    public async Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId)
    {
        var key = $"qb:preset:{interviewPresetId}";

        if (cache.TryGetValue(key, out InterviewPresetApiDto? cached))
            return cached;

        var result = await inner.GetPresetAsync(interviewPresetId);
        if (result is not null)
            cache.Set(key, result, PresetTtl);

        return result;
    }
}