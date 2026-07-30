using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Interfaces;
using SocialAPI.Repositories;
using SocialAPI.Services;
using MissionAPI.Protos;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddGrpc();

var socialConn = Environment.GetEnvironmentVariable("SOCIAL_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("SocialConnection");
if (!string.IsNullOrEmpty(socialConn))
{
    builder.Services.AddDbContext<SocialDbContext>(options => options.UseSqlServer(socialConn));
}

builder.Services.AddHttpClient<IComicValidator, ComicValidator>(client =>
{
    var apiGatewayUrl = Environment.GetEnvironmentVariable("API_GATEWAY_URL") ?? "http://localhost:5000";
    client.BaseAddress = new Uri(apiGatewayUrl);
});
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IMissionProgressNotifier, MissionProgressNotifier>();

var missionApiUrl = Environment.GetEnvironmentVariable("MISSION_API_URL")
    ?? builder.Configuration["GrpcSettings:MissionApiUrl"]
    ?? "https://localhost:7224";
builder.Services.AddGrpcClient<MissionProgress.MissionProgressClient>(o =>
{
    o.Address = new Uri(missionApiUrl);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(secretKey)) secretKey = "super_secret_key_for_development_purposes_only_replace_this!";

var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["JwtSettings:Issuer"];
if (string.IsNullOrEmpty(issuer)) issuer = "prn232_comic_api";

var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["JwtSettings:Audience"];
if (string.IsNullOrEmpty(audience)) audience = "prn232_comic_api";

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
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey))
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<SocialDbContext>();
    db?.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS is terminated at ApiGateway; internal service traffic stays on HTTP.

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<SocialAPI.Services.SocialActivityGrpcService>();
app.MapControllers();

app.Run();
