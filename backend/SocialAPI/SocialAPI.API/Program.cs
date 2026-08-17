using Microsoft.EntityFrameworkCore;
using SocialAPI.Data;
using SocialAPI.Interfaces;
using SocialAPI.Repositories;
using SocialAPI.Services;
using SharedKernel.Extensions;
using MissionAPI.Protos;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddGrpc();

var socialConn = Environment.GetEnvironmentVariable("SOCIAL_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("SocialConnection");
if (string.IsNullOrEmpty(socialConn))
{
    socialConn = "Server=localhost;Database=SocialDB;Trusted_Connection=True;TrustServerCertificate=True;";
}
builder.Services.AddDbContext<SocialDbContext>(options => options.UseSqlServer(socialConn));

builder.Services.AddHttpClient<IComicValidator, ComicValidator>(client =>
{
    var apiGatewayUrl = Environment.GetEnvironmentVariable("API_GATEWAY_URL");
    if (string.IsNullOrWhiteSpace(apiGatewayUrl))
    {
        apiGatewayUrl = builder.Configuration["ApiGateway:BaseUrl"];
    }
    if (string.IsNullOrWhiteSpace(apiGatewayUrl))
    {
        apiGatewayUrl = "http://127.0.0.1:5028";
    }

    client.BaseAddress = new Uri(apiGatewayUrl);
});
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IReadingHistoryRepository, ReadingHistoryRepository>();
builder.Services.AddScoped<ISocialActivityRepository, SocialActivityRepository>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SocialAPI.Application.Features.Comments.Commands.CreateCommentCommand).Assembly));
builder.Services.AddScoped<IMissionProgressNotifier, MissionProgressNotifier>();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<SocialAPI.Data.SocialDbContext>(o =>
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<SocialDbContext>();
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

app.MapGrpcService<SocialAPI.GrpcServices.SocialActivityGrpcService>();
app.MapControllers();

app.Run();
