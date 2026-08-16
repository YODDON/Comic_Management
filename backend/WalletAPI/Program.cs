using Microsoft.EntityFrameworkCore;
using WalletAPI.Data;
using WalletAPI.Interfaces;
using WalletAPI.Repositories;
using WalletAPI.Services;
using SharedKernel.Extensions;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddGrpc();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IWithdrawRepository, WithdrawRepository>();
builder.Services.AddScoped<IWithdrawService, WithdrawService>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletApplicationService, WalletApplicationService>();

var walletConn = Environment.GetEnvironmentVariable("WALLET_DB_CONNECTION");
if (string.IsNullOrEmpty(walletConn)) walletConn = builder.Configuration.GetConnectionString("WalletConnection");
if (string.IsNullOrEmpty(walletConn)) walletConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(walletConn)) walletConn = "Server=localhost,1433;Database=WalletDB;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

if (!string.IsNullOrEmpty(walletConn))
{
    builder.Services.AddDbContext<WalletDbContext>(options => options.UseSqlServer(walletConn));
}

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<WalletDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    x.AddConsumer<WalletAPI.Consumers.MissionRewardGrantedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitmqHost, "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("mission-reward-granted", e =>
        {
            e.ConfigureConsumer<WalletAPI.Consumers.MissionRewardGrantedConsumer>(context);
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<WalletDbContext>();
    db?.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS is terminated at ApiGateway; internal service traffic stays on HTTP.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<WalletAPI.GrpcServices.WalletGrpcService>();

app.Run();
