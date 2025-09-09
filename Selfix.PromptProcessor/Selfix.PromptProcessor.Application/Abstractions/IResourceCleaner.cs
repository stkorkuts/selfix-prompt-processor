using LanguageExt;

namespace Selfix.PromptProcessor.Application.Abstractions;

public interface IResourceCleaner
{
    public IO<Unit> Cleanup(CancellationToken cancellationToken);
}