using Microsoft.EntityFrameworkCore;
using WalletAPI.Data;
using WalletAPI.Interfaces;
using WalletAPI.Repositories;
using WalletAPI.Services;

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

var walletConn = Environment.GetEnvironmentVariable("WALLET_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("WalletConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(walletConn))
{
    builder.Services.AddDbContext<WalletDbContext>(options => options.UseSqlServer(walletConn));
}

var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret")
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:Secret"]
    ?? "super_secret_key_for_development_purposes_only_replace_this!";
var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer")
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "prn232_comic_api";
var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience")
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "prn232_comic_api";

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(secretKey))
        };
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
app.MapGrpcService<WalletAPI.Services.WalletGrpcService>();

app.Run();
