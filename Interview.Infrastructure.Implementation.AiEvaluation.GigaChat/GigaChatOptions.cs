namespace Interview.Infrastructure.Implementation.AiEvaluation.GigaChat;

internal class GigaChatOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string TheoryModel { get; set; } = "GigaChat-2";
    public string CodingModel { get; set; } = "GigaChat-2";
    public string SessionModel { get; set; } = "GigaChat-2";

    public decimal Temperature { get; set; } = 0.1m;
    
    public int MaxTokensTheory { get; set; } = 350;
    public int MaxTokensCoding { get; set; } = 450;
    public int MaxTokensSession { get; set; } = 1000;
}