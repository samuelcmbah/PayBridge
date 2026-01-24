using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayBridge.Application.IServices;
using PayBridge.Application.Services;
using PayBridge.Infrastructure.ExternalServices;
using PayBridge.Infrastructure.PayStack;
using PayBridge.Infrastructure.Persistence;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));
//builder.Services.Configure<FlutterwaveSettings>(
//    builder.Configuration.GetSection("Flutterwave"));

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
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentGateway, PaystackGateway>();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
