using System.Text;
using SharedKernel.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentAPI.Data;
using PaymentAPI.Interfaces;
using PaymentAPI.Repositories;
using PaymentAPI.Services;
using ChapterAPI.Protos;
using WalletAPI.Protos;
using Microsoft.Extensions.Options;
using PaymentAPI.Settings;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

var paymentConn = Environment.GetEnvironmentVariable("PAYMENT_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("PaymentConnection");
if (!string.IsNullOrEmpty(paymentConn))
{
    builder.Services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer(paymentConn));
}

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var chapterApiUrl = Environment.GetEnvironmentVariable("CHAPTER_API_URL") ?? builder.Configuration["GrpcEndpoints:ChapterAPI"] ?? "https://localhost:7147";
builder.Services.AddGrpcClient<ChapterGrpc.ChapterGrpcClient>(o =>
{
    o.Address = new Uri(chapterApiUrl);
});
var walletApiUrl = Environment.GetEnvironmentVariable("WALLET_API_URL") ?? builder.Configuration["GrpcEndpoints:WalletAPI"] ?? "http://localhost:5091";
builder.Services.AddGrpcClient<WalletService.WalletServiceClient>(o => o.Address = new Uri(walletApiUrl));

builder.Services.Configure<TopUpSettings>(builder.Configuration.GetSection(TopUpSettings.SectionName));
builder.Services.Configure<BankSettings>(builder.Configuration.GetSection(BankSettings.SectionName));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<PaymentDbContext>();
    db?.Database.Migrate();
}

var topUp = app.Services.GetRequiredService<IOptions<TopUpSettings>>().Value;
app.Logger.LogInformation("Top-up configured. CoinRate={CoinRate}", topUp.CoinRate);

var bank = app.Services.GetRequiredService<IOptions<BankSettings>>().Value;
if (!bank.IsWebhookConfigured)
{
    // The webhook now rejects everything rather than trusting anyone, so deposits simply stop.
    app.Logger.LogWarning(
        "BankSettings__SepayApiKey is not set. The SePay webhook will reject every call and no deposit can be credited.");
}

if (!bank.IsBankConfigured)
{
    app.Logger.LogWarning(
        "BankSettings__BankCode / BankSettings__AccountNumber are not set. Deposit QR generation is unavailable.");
}
// SepayApiKey is a secret and is never logged.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS is terminated at ApiGateway; internal service traffic stays on HTTP.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
