using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Interview.Infrastructure.Implementation.AiEvaluation.GigaChat.Contracts;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Coding;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.AiEvaluation.Theory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

internal class GigaChatAiEvaluationService(
    HttpClient httpClient,
    IGigaChatTokenClient tokenClient,
    IOptions<GigaChatOptions> options,
    ILogger<GigaChatAiEvaluationService> logger) : IAiEvaluationService
{
    private readonly GigaChatOptions _options = options.Value;
    
    public async Task<TheoryEvaluationResult> EvaluateTheoryAsync(TheoryEvaluationRequest request, CancellationToken ct = default)
    {
        var systemPrompt = GigaChatPromptFactory.BuildTheorySystemPrompt();
        var userPrompt = GigaChatPromptFactory.BuildTheoryUserPrompt(request);
        
        var result = await SendAndParseTheoryAsync(systemPrompt, userPrompt, ct);
        if (result is not null) return result;
        
        var retry = userPrompt + "\n\nВерни строго валидный JSON";
        result = await SendAndParseTheoryAsync(systemPrompt, retry, ct);

        return result ?? throw new InvalidOperationException("Invalid theory JSON from GigaChat.");
    }

    public async Task<CodingEvaluationResult> EvaluateCodingAsync(CodingEvaluationRequest request, CancellationToken ct = default)
    {
        var systemPrompt = GigaChatPromptFactory.BuildCodingSystemPrompt();
        var userPrompt = GigaChatPromptFactory.BuildCodingUserPrompt(request);
        
        var result = await SendAndParseCodingAsync(systemPrompt, userPrompt, ct);
        if (result is not null) return result;
        
        var retry = userPrompt + "\n\nВерни строго валидный JSON";
        result = await SendAndParseCodingAsync(systemPrompt, retry, ct);

        return result ?? throw new InvalidOperationException("Invalid coding JSON from GigaChat.");
    }

    public async Task<SessionEvaluationResult> EvaluateSessionAsync(SessionEvaluationRequest request, CancellationToken ct = default)
    {
        var systemPrompt = GigaChatPromptFactory.BuildSessionSystemPrompt();
        var userPrompt = GigaChatPromptFactory.BuildSessionUserPrompt(request);

        var result = await SendAndParseSessionAsync(systemPrompt, userPrompt, ct);
        if (result is not null) return result;

        var retry = userPrompt + "\n\nВерни строго валидный JSON";
        result = await SendAndParseSessionAsync(systemPrompt, retry, ct);

        return result ?? throw new InvalidOperationException("Invalid session JSON from GigaChat.");
    }

    private async Task<string?> SendAsync(string model, string systemPrompt, string userPrompt, int maxTokens, CancellationToken ct)
    {
        var payload = new ChatCompletionRequest
        {
            Model = model,
            Messages =
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            Temperature = _options.Temperature,
            MaxTokens = maxTokens,
            Stream = false
        };
        
        var payloadJson = JsonSerializer.Serialize(payload);
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        var accessToken = await tokenClient.GetAccessTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        logger.LogInformation(
            "Sending GigaChat request. Model: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}",
            model,
            maxTokens,
            _options.Temperature);
        
        logger.LogInformation("GigaChat request payload: {Payload}", payloadJson);
        
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        
        var body = await response.Content.ReadAsStringAsync(ct);
        
        logger.LogInformation(
            "Received GigaChat HTTP response. StatusCode: {StatusCode}, Body: {Body}",
            (int)response.StatusCode,
            body);
        
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

        logger.LogInformation(
            "Extracted GigaChat content: {Content}",
            content);

        return content;
    }
    
    private async Task<TheoryEvaluationResult?> SendAndParseTheoryAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var content = await SendAsync(_options.TheoryModel, systemPrompt, userPrompt, _options.MaxTokensTheory, ct);
        if (string.IsNullOrWhiteSpace(content)) return null;
        
        TheoryResponseJson? parsed;
        try { parsed = JsonSerializer.Deserialize<TheoryResponseJson>(content); }
        catch { return null; }

        if (parsed is null || parsed.Score is < 0 or > 10 || string.IsNullOrWhiteSpace(parsed.Feedback))
            return null;

        return new TheoryEvaluationResult(parsed.Score, parsed.Feedback, content);
    }
    
    private async Task<CodingEvaluationResult?> SendAndParseCodingAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var content = await SendAsync(_options.CodingModel, systemPrompt, userPrompt, _options.MaxTokensCoding, ct);
        if (string.IsNullOrWhiteSpace(content)) return null;
        
        CodingResponseJson? parsed;
        try { parsed = JsonSerializer.Deserialize<CodingResponseJson>(content); }
        catch { return null; }

        if (parsed is null || parsed.Score is < 0 or > 10 || string.IsNullOrWhiteSpace(parsed.Feedback))
            return null;

        return new CodingEvaluationResult(parsed.Score, parsed.Feedback, content);
    }
    
    private async Task<SessionEvaluationResult?> SendAndParseSessionAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var content = await SendAsync(_options.SessionModel, systemPrompt, userPrompt, _options.MaxTokensSession, ct);
        if (string.IsNullOrWhiteSpace(content)) return null;

        SessionResponseJson? parsed;
        try { parsed = JsonSerializer.Deserialize<SessionResponseJson>(content); }
        catch { return null; }

        if (string.IsNullOrWhiteSpace(parsed?.Summary))
            return null;

        return new SessionEvaluationResult(
            parsed.Summary,
            parsed.Strengths?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [],
            parsed.Weaknesses?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [],
            parsed.Recommendations?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [],
            content);
    }
    
    private record TheoryResponseJson
    {
        [JsonPropertyName("score")] public int Score { get; init; }
        [JsonPropertyName("feedback")] public string Feedback { get; init; } = string.Empty;
    }
    
    private record CodingResponseJson
    {
        [JsonPropertyName("score")] public int Score { get; init; }
        [JsonPropertyName("feedback")] public string Feedback { get; init; } = string.Empty;
    }
    
    private record SessionResponseJson
    {
        [JsonPropertyName("summary")] public string Summary { get; init; } = string.Empty;
        [JsonPropertyName("strengths")] public List<string>? Strengths { get; init; }
        [JsonPropertyName("weaknesses")] public List<string>? Weaknesses { get; init; }
        [JsonPropertyName("recommendations")] public List<string>? Recommendations { get; init; }
    }
}