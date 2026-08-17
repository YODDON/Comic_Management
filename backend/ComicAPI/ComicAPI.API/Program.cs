using Microsoft.EntityFrameworkCore;
using ComicAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SharedKernel.Extensions;
using MassTransit;
using ComicAPI.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

var comicConn = Environment.GetEnvironmentVariable("COMIC_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("ComicConnection");
if (!string.IsNullOrEmpty(comicConn))
{
    builder.Services.AddDbContext<ComicDbContext>(options => options.UseSqlServer(comicConn));
}

var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConn;
    options.InstanceName = "ComicAPI_";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste your JWT Token here (without the 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
builder.Services.Configure<ComicAPI.Infrastructure.Settings.CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ComicAPI.Application.Interfaces.ICloudinaryService, ComicAPI.Infrastructure.Services.CloudinaryService>();
builder.Services.Configure<ComicAPI.Infrastructure.Settings.N8nTranslationSettings>(
    builder.Configuration.GetSection(ComicAPI.Infrastructure.Settings.N8nTranslationSettings.SectionName));
builder.Services.AddHttpClient<ComicAPI.Application.Interfaces.ITranslationService, ComicAPI.Infrastructure.Services.N8nTranslationService>();

builder.Services.AddAutoMapper(typeof(ComicAPI.Application.Mappings.ComicProfile).Assembly);

var userApiUrl = Environment.GetEnvironmentVariable("USER_API_URL") ?? builder.Configuration["GrpcSettings:UserApiUrl"] ?? "http://localhost:5054";
builder.Services.AddGrpcClient<UserAPI.Protos.UserService.UserServiceClient>(o =>
{
    o.Address = new Uri(userApiUrl);
});

var chapterApiUrl = Environment.GetEnvironmentVariable("CHAPTER_API_URL")
    ?? builder.Configuration["GrpcEndpoints:ChapterAPI"]
    ?? "http://localhost:5115";
builder.Services.AddGrpcClient<ChapterAPI.Protos.ChapterGrpc.ChapterGrpcClient>(o =>
{
    o.Address = new Uri(chapterApiUrl);
});

builder.Services.AddScoped<ComicAPI.Domain.Interfaces.ICategoryRepository, ComicAPI.Infrastructure.Repositories.CategoryRepository>();
builder.Services.AddScoped<ComicAPI.Application.Interfaces.ICategoryService, ComicAPI.Application.Services.CategoryService>();
builder.Services.AddScoped<ComicAPI.Domain.Interfaces.IComicRepository, ComicAPI.Infrastructure.Repositories.ComicRepository>();
builder.Services.AddScoped<ComicAPI.Application.Interfaces.IComicService, ComicAPI.Application.Services.ComicService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ComicViewedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitmqHost, "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("comic-viewed", e =>
        {
            e.ConfigureConsumer<ComicViewedEventConsumer>(context);
        });
    });
});

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<ComicDbContext>();
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
