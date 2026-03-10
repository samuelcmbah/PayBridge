using MassTransit;
using Microsoft.Extensions.Options;
using PayBridge.Infrastructure.Messaging;
using PayBridge.NotificationWorker.Consumers;
using PayBridge.NotificationWorker.Settings;
using Serilog;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Services.AddSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("MassTransit", Serilog.Events.LogEventLevel.Information)
        .WriteTo.Console();
});

// ── Settings ──────────────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.Configure<NotificationWorkerSettings>(
    builder.Configuration.GetSection("NotificationWorker"));

// ── HttpClient ────────────────────────────────────────────────────────────────
// Named "NotificationClient" — used by the consumer to POST to app endpoints.
// Timeout is intentionally short; the retry policy handles repeated attempts.
builder.Services.AddHttpClient<PaymentSucceededConsumer>((serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<IOptions<NotificationWorkerSettings>>()
        .Value;

    client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
    client.DefaultRequestHeaders.Add("User-Agent", "PayBridge-NotificationWorker/1.0");
});

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentSucceededConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSettings = context
            .GetRequiredService<IOptions<RabbitMqSettings>>()
            .Value;

        var workerSettings = context
            .GetRequiredService<IOptions<NotificationWorkerSettings>>()
            .Value;

        cfg.Host(rabbitSettings.Host, rabbitSettings.VirtualHost, h =>
        {
            h.Username(rabbitSettings.Username);
            h.Password(rabbitSettings.Password);
        });

        // Configure the receive endpoint (this becomes the queue name in RabbitMQ)
        cfg.ReceiveEndpoint("payment-succeeded", (IRabbitMqReceiveEndpointConfigurator e) =>
        {
            // Queue survives RabbitMQ restarts — critical for production
            e.Durable = true;

            // If the consumer throws, MassTransit retries with exponential backoff.
            // Attempt 1: 30s, Attempt 2: 60s, Attempt 3: 120s ... up to MaxRetryAttempts.
            // After all attempts are exhausted, the message moves to the dead-letter queue.
            e.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit: workerSettings.MaxRetryAttempts,
                    minInterval: TimeSpan.FromSeconds(workerSettings.RetryIntervalSeconds),
                    maxInterval: TimeSpan.FromHours(2),
                    intervalDelta: TimeSpan.FromSeconds(workerSettings.RetryIntervalSeconds));

                // Do not retry if the message itself is malformed — retrying won't help
                r.Ignore<JsonException>();
            });

            // ── Dead Letter / Error Queue ────────────────────────────────────
            // MassTransit will create the error and _error queues automatically (e.g. "payment-succeeded_error").
            // If you need custom dead-letter bindings, use e.BindDeadLetterQueue(...) or appropriate APIs.

            e.ConfigureConsumer<PaymentSucceededConsumer>(context);

            
        });
    });
});

var host = builder.Build();
host.Run();