using Microsoft.EntityFrameworkCore;
using ChapterAPI.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Extensions;
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
if (string.IsNullOrEmpty(chapterConn))
{
    chapterConn = "Server=localhost;Database=ChapterDB;Trusted_Connection=True;TrustServerCertificate=True;";
}
builder.Services.AddDbContext<ChapterDbContext>(options => options.UseSqlServer(chapterConn));

var apiGatewayUrl = Environment.GetEnvironmentVariable("API_GATEWAY_URL") 
    ?? builder.Configuration["ApiGateway:BaseUrl"] 
    ?? "http://localhost:5028";
builder.Services.AddHttpClient<IComicValidator, ComicValidator>(client =>
{
    client.BaseAddress = new Uri(apiGatewayUrl + "/api");
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IMissionProgressNotifier, MissionProgressNotifier>();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<ChapterAPI.Data.ChapterDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitmqHost, "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

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
