using LanguageExt;

namespace Selfix.PromptProcessor.Application.Abstractions;

public interface IUseCase<in TRequest, TResponse>
{
    IO<TResponse> Execute(TRequest request, CancellationToken cancellationToken);
}