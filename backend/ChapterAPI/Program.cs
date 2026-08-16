using Microsoft.EntityFrameworkCore;
using ChapterAPI.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChapterAPI.Interfaces;
using ChapterAPI.Repositories;
using ChapterAPI.Services;
using MissionAPI.Protos;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddGrpc();

builder.Services.Configure<ChapterAPI.Settings.CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ChapterAPI.Interfaces.ICloudinaryService, ChapterAPI.Services.CloudinaryService>();

var chapterConn = Environment.GetEnvironmentVariable("CHAPTER_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("ChapterConnection");
if (!string.IsNullOrEmpty(chapterConn))
{
    builder.Services.AddDbContext<ChapterDbContext>(options => options.UseSqlServer(chapterConn));
}

var comicApiUrl = Environment.GetEnvironmentVariable("COMIC_API_URL") ??
    builder.Configuration["GrpcSettings:ComicApiUrl"] ?? "https://localhost:7024";
builder.Services.AddGrpcClient<ChapterAPI.Protos.ComicGrpc.ComicGrpcClient>(o =>
{
    o.Address = new Uri(comicApiUrl);
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IMissionProgressNotifier, MissionProgressNotifier>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitmqHost, "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(secretKey)) secretKey = "super_secret_key_for_development_purposes_only_replace_this!";

var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["JwtSettings:Issuer"];
if (string.IsNullOrEmpty(issuer)) issuer = "prn232_comic_api";

var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["JwtSettings:Audience"];
if (string.IsNullOrEmpty(audience)) audience = "prn232_comic_api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<ChapterDbContext>();
    db?.Database.Migrate();
}

app.MapGrpcService<ChapterAPI.GrpcServices.ChapterGrpcService>();

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
