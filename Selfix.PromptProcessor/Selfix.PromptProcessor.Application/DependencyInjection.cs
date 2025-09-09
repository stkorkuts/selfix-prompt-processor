using Microsoft.Extensions.DependencyInjection;
using Selfix.PromptProcessor.Application.UseCases.ProcessPrompt;

namespace Selfix.PromptProcessor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection collection) =>
        collection.AddTransient<ProcessPromptUseCase>();
}