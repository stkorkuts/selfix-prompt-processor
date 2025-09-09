using Selfix.PromptProcessor.Application.Abstractions;

namespace Selfix.PromptProcessor.Infrastructure;

internal sealed class EnvironmentService : IEnvironmentService
{
    public string BaseDirectory => AppContext.BaseDirectory;
}