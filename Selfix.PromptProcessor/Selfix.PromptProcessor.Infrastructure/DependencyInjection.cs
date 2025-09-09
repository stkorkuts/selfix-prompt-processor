using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Selfix.PromptProcessor.Application.Abstractions;
using Selfix.PromptProcessor.Infrastructure.EventStreaming;
using Selfix.PromptProcessor.Infrastructure.PromptProcessing;
using Selfix.PromptProcessor.Shared.Settings;
using Selfix.Schema.Kafka.Jobs.Images.V1.PromptProcessing;
using Serilog;

namespace Selfix.PromptProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection collection, KafkaSettings kafkaSettings) =>
        collection
            .AddSingleton<IPromptProcessor, VllmPromptProcessor>()
            .AddSingleton<IEnvironmentService, EnvironmentService>()
            .AddKafka(kafkaSettings);

    private static IServiceCollection AddKafka(this IServiceCollection collection, KafkaSettings kafkaSettings) =>
        collection.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();
            configurator.AddSerilog();
            
            configurator.AddConfigureEndpointsCallback((_,_,cfg) =>
            {
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            });
            
            configurator.UsingInMemory();
            configurator.AddRider(rider =>
            {
                rider.AddConsumer<PromptProcessingConsumer>();
                rider.AddProducer<ProcessPromptResponseEvent>(kafkaSettings.TopicOutput);

                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(kafkaSettings.BootstrapServer, host => host.UseSasl(sasl =>
                    {
                        SaslSettings saslSettings = kafkaSettings.Sasl;

                        sasl.Username = saslSettings.Username;
                        sasl.Password = saslSettings.Password;
                        sasl.Mechanism = Enum.Parse<SaslMechanism>(saslSettings.KafkaSaslMechanism, true);
                        sasl.SecurityProtocol = Enum.Parse<SecurityProtocol>(saslSettings.KafkaSecurityProtocol, true);
                    }));

                    kafka.TopicEndpoint<ProcessPromptRequestEvent>(kafkaSettings.TopicInput, 
                        $"{kafkaSettings.GroupId}-{kafkaSettings.TopicInput}",
                        endpointConfigurator =>
                        {
                            endpointConfigurator.ConfigureConsumer<PromptProcessingConsumer>(context);
                            endpointConfigurator.AutoOffsetReset = AutoOffsetReset.Earliest;
                        });
                });
            });
        });
}