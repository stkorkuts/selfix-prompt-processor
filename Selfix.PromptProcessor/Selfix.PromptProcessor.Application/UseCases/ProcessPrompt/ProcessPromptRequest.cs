namespace Selfix.PromptProcessor.Application.UseCases.ProcessPrompt;

public sealed record ProcessPromptRequest(long Seed, string AvatarDescription, string RawPrompt);