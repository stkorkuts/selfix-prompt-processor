using LanguageExt;

namespace Selfix.PromptProcessor.Application.Abstractions;

public interface IPromptProcessor
{
    OptionT<IO, string> Process(string prompt, long seed, CancellationToken cancellationToken);
}