using Microsoft.EntityFrameworkCore;
using PayBridge.Application.IServices;
using PayBridge.Application.Services;
using PayBridge.Infrastructure.ExternalServices;
using PayBridge.Infrastructure.PayStack;
using PayBridge.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Bind the appsettings section to the PaystackSettings class
builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));
// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// If using the Factory approach:
builder.Services.AddHttpClient();
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
