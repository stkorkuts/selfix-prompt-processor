﻿using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Selfix.PromptProcessor.Application;
using Selfix.PromptProcessor.EntryPoint.Extensions;
using Selfix.PromptProcessor.Infrastructure;
using Selfix.PromptProcessor.Shared.Settings;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    Log.Information("Starting application");
    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    builder
        .AddSettings<KafkaSettings>("Kafka")
        .AddSettings<VllmSettings>("Vllm");

    ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
    KafkaSettings kafkaSettings = serviceProvider.GetRequiredService<IOptions<KafkaSettings>>().Value;

    builder.Logging.AddSerilog(new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(serviceProvider)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger());

    builder.Services
        .AddApplication()
        .AddInfrastructure(kafkaSettings);

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}