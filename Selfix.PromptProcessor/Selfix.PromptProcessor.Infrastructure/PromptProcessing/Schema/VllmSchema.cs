using System.Text.Json.Serialization;

namespace Selfix.PromptProcessor.Infrastructure.PromptProcessing.Schema;

internal sealed class Message
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

internal sealed class VllmChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required Message[] Messages { get; init; }

    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; init; }

    [JsonPropertyName("temperature")]
    public required double Temperature { get; init; }

    [JsonPropertyName("seed")]
    public required long Seed { get; init; }
    
    [JsonPropertyName("stop")]
    public required string[] Stop { get; init; } = [];
}

internal sealed class ChatChoice
{
    [JsonPropertyName("message")]
    public required Message Message { get; init; }

    [JsonPropertyName("finish_reason")]
    public required string FinishReason { get; init; }

    [JsonPropertyName("index")]
    public required int Index { get; init; }
}

internal sealed class VllmChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public required ChatChoice[] Choices { get; init; } = [];
}

internal sealed class ModelData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("object")]
    public required string Object { get; init; }
    
    [JsonPropertyName("created")]
    public required int Created { get; init; }
    
    [JsonPropertyName("owned_by")]
    public required string OwnedBy { get; init; }
}

internal sealed class ModelsResponse
{
    [JsonPropertyName("data")]
    public required ModelData[] Data { get; init; } = [];
    
    [JsonPropertyName("object")]
    public required string Object { get; init; }
}