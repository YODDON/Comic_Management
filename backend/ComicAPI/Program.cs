using Microsoft.EntityFrameworkCore;
using ComicAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

var comicConn = Environment.GetEnvironmentVariable("COMIC_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("ComicConnection");
if (!string.IsNullOrEmpty(comicConn))
{
    builder.Services.AddDbContext<ComicDbContext>(options => options.UseSqlServer(comicConn));
}

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
builder.Services.Configure<ComicAPI.Settings.CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ComicAPI.Interfaces.ICloudinaryService, ComicAPI.Services.CloudinaryService>();
builder.Services.Configure<ComicAPI.Settings.N8nTranslationSettings>(
    builder.Configuration.GetSection(ComicAPI.Settings.N8nTranslationSettings.SectionName));
builder.Services.AddHttpClient<ComicAPI.Interfaces.ITranslationService, ComicAPI.Services.N8nTranslationService>();

builder.Services.AddAutoMapper(typeof(ComicAPI.Mappings.ComicProfile).Assembly);

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

builder.Services.AddScoped<ComicAPI.Interfaces.ICategoryRepository, ComicAPI.Repositories.CategoryRepository>();
builder.Services.AddScoped<ComicAPI.Interfaces.ICategoryService, ComicAPI.Services.CategoryService>();
builder.Services.AddScoped<ComicAPI.Interfaces.IComicRepository, ComicAPI.Repositories.ComicRepository>();
builder.Services.AddScoped<ComicAPI.Interfaces.IComicService, ComicAPI.Services.ComicService>();

builder.Services.AddGrpc();

var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrEmpty(secretKey)) secretKey = "SuperSecretKeyForDevelopmentOnly123!AndItNeedsToBeAtLeast32BytesLong!";

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<ComicDbContext>();
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

app.MapControllers();
app.MapGrpcService<ComicAPI.Services.ComicGrpcService>();

app.Run();
