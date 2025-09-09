namespace Selfix.PromptProcessor.Shared.Settings;

public sealed class VllmSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required int MaxTokens { get; init; }
    public required double Temperature { get; init; }
    public required bool IsHighVram { get; init; }
    public required string LoraKeyWord { get; init; }
}