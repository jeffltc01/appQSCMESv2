namespace MESv2.Api.Services;

public class NaturalLanguageQueryOptions
{
    public bool Enabled { get; set; } = true;
    public bool EnablePlantWideQueries { get; set; } = true;
    public int MaxQuestionLength { get; set; } = 500;
    public int MaxAnswerChars { get; set; } = 1200;
    public int MaxFollowUps { get; set; } = 3;
    public int MaxDataPoints { get; set; } = 10;
    public int IntentCacheSeconds { get; set; } = 120;
    public string Provider { get; set; } = "mock";
    public string[] DisabledIntents { get; set; } = [];
    public HttpPrivateLlmClientOptions HttpModel { get; set; } = new();
}

public class HttpPrivateLlmClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string InterpretPath { get; set; } = "/v1/nlq/interpret";
}
