using Microsoft.EntityFrameworkCore;
using BannerAPI.Data;
using BannerAPI.Interfaces;
using BannerAPI.Repositories;
using BannerAPI.Services;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

var bannerConn = Environment.GetEnvironmentVariable("BANNER_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("BannerConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(bannerConn))
{
    builder.Services.AddDbContext<BannerDbContext>(options => options.UseSqlServer(bannerConn));
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
    var db = scope.ServiceProvider.GetService<BannerDbContext>();
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

app.Run();
