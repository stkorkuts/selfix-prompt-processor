using LanguageExt;
using MassTransit;
using Selfix.PromptProcessor.Application.UseCases.ProcessPrompt;
using Selfix.Schema.Kafka.Jobs.Images.V1.PromptProcessing;

namespace Selfix.PromptProcessor.Infrastructure.EventStreaming;

public sealed class PromptProcessingConsumer : IConsumer<ProcessPromptRequestEvent>
{
    private readonly ProcessPromptUseCase _useCase;
    private readonly ITopicProducer<ProcessPromptResponseEvent> _topicProducer;

    public PromptProcessingConsumer(
        ProcessPromptUseCase useCase,
        ITopicProducer<ProcessPromptResponseEvent> topicProducer)
    {
        _useCase = useCase;
        _topicProducer = topicProducer;
    }

    public async Task Consume(ConsumeContext<ProcessPromptRequestEvent> context)
    {
        ProcessPromptRequestEvent message = context.Message;
        ProcessPromptRequest request = new(message.Seed, message.AvatarDescription, message.RawPrompt);

        Fin<ProcessPromptResponse> result = await _useCase
            .Execute(request, context.CancellationToken)
            .RunSafeAsync();

        ProcessPromptResponseEvent kafkaResponse = result.Match(
            Succ: response => new ProcessPromptResponseEvent
            {
                JobId = message.JobId,
                Success = new ProcessPromptResponseEventSuccessData { Prompt = response.Text },
                IsSuccess = true
            },
            Fail: error => new ProcessPromptResponseEvent
            {
                JobId = message.JobId,
                Fail = new ProcessPromptResponseEventFailData { Error = error.ToString() },
                IsSuccess = false
            });
        
        await _topicProducer.Produce(kafkaResponse, context.CancellationToken);
    }
}