using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayBridge.API.ExceptionHandlers;
using PayBridge.Application.IServices;
using PayBridge.Application.Services;
using PayBridge.Application.Validators;
using PayBridge.Infrastructure.ExternalServices;
using PayBridge.Infrastructure.Messaging;
using PayBridge.Infrastructure.PayStack;
using PayBridge.Infrastructure.Persistence;
using Serilog;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Replace default logging with Serilog
builder.Host.UseSerilog((_, logger) =>
{
    logger
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console();
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // Enables ProblemDetails RFC 7807

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<PaymentRequestValidator>();

builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));
//builder.Services.Configure<FlutterwaveSettings>(
//    builder.Configuration.GetSection("Flutterwave"));

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// register httpclient config here, not in constructor
builder.Services.AddHttpClient<PaystackHttpClient>((serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<IOptions<PaystackSettings>>()
        .Value;

    client.BaseAddress = new Uri(settings.BaseUrl);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", settings.SecretKey);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqSettings = context
            .GetRequiredService<IOptions<RabbitMqSettings>>()
            .Value;

        cfg.Host(rabbitMqSettings.Host, (ushort)rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });

        // No consumers registered here - this is publish-only
        // Consumers live in PayBridge.NotificationWorker
        // MassTransit will automatically create the exchange for PaymentSucceededEvent
    });
});

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentGateway, PaystackGateway>();
// AppNotificationService is kept for now but will no longer be called directly
// by PaymentService - the NotificationWorker takes over that responsibility
builder.Services.AddScoped<IAppNotificationService, AppNotificationService>();



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseSerilogRequestLogging();

app.Run();
