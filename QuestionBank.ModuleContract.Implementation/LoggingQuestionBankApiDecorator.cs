using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuestionBank.InternalApi;

namespace QuestionBank.ModuleContract.Implementation;

internal sealed class LoggingQuestionBankApiDecorator(
    IQuestionBankApi inner,
    ILogger<LoggingQuestionBankApiDecorator> logger) : IQuestionBankApi
{
    public async Task<IReadOnlyList<InterviewQuestionApiDto>> GetQuestionsAsync(Guid interviewPresetId, int totalQuestions)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await inner.GetQuestionsAsync(interviewPresetId, totalQuestions);

            logger.LogInformation(
                "QuestionBankApi.GetQuestionsAsync presetId={PresetId} requested={Requested} returned={Returned} elapsedMs={ElapsedMs}",
                interviewPresetId, totalQuestions, result.Count, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "QuestionBankApi.GetQuestionsAsync failed presetId={PresetId} requested={Requested} elapsedMs={ElapsedMs}",
                interviewPresetId, totalQuestions, sw.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<InterviewPresetApiDto?> GetPresetAsync(Guid interviewPresetId)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await inner.GetPresetAsync(interviewPresetId);

            logger.LogInformation(
                "QuestionBankApi.GetPresetAsync presetId={PresetId} found={Found} elapsedMs={ElapsedMs}",
                interviewPresetId, result is not null, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "QuestionBankApi.GetPresetAsync failed presetId={PresetId} elapsedMs={ElapsedMs}",
                interviewPresetId, sw.ElapsedMilliseconds);

            throw;
        }
    }
}
