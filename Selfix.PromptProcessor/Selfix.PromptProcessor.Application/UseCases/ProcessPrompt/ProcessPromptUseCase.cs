using LanguageExt;
using Selfix.PromptProcessor.Application.Abstractions;
using Selfix.PromptProcessor.Shared.Extensions;
using Serilog;

namespace Selfix.PromptProcessor.Application.UseCases.ProcessPrompt;

public sealed class ProcessPromptUseCase : IUseCase<ProcessPromptRequest, ProcessPromptResponse>
{
    private readonly IPromptProcessor _promptProcessor;

    public ProcessPromptUseCase(IPromptProcessor promptProcessor)
    {
        _promptProcessor = promptProcessor;
    }

    public IO<ProcessPromptResponse> Execute(ProcessPromptRequest request, CancellationToken cancellationToken)
    {
        var lines = request.AvatarDescription.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var linesTruncated = string.Join("\n", lines.Take(2));

        var truncatedDescription = linesTruncated.Length > 512
            ? string.Concat(linesTruncated.AsSpan(0, 512), "...")
            : linesTruncated;

        return _promptProcessor
            .Process(
                $"User's textual avatar description from computer vision: {truncatedDescription}\nUser's input prompt in Russian: {request.RawPrompt}",
                request.Seed, cancellationToken)
            .ToIOOrFail("Prompt processor did not return a result")
            .WithLogging(
                () => Log.Information("Start processing prompt"),
                () => Log.Information("Prompt processed successfully"),
                err => Log.Error(err, "Processing failed"))
            .Map(text => new ProcessPromptResponse(text));
    }
}