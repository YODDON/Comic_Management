using Microsoft.EntityFrameworkCore;
using MissionAPI.Data;
using ChapterAPI.Protos;
using SocialAPI.Protos;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddGrpc();

var missionConn = Environment.GetEnvironmentVariable("MISSION_DB_CONNECTION") ?? builder.Configuration.GetConnectionString("MissionConnection");
if (!string.IsNullOrEmpty(missionConn))
{
    builder.Services.AddDbContext<MissionDbContext>(options => options.UseSqlServer(missionConn));
}

builder.Services.AddScoped<MissionAPI.Interfaces.IMissionRepository, MissionAPI.Repositories.MissionRepository>();
builder.Services.AddScoped<MissionAPI.Interfaces.IMissionService, MissionAPI.Services.MissionService>();
builder.Services.AddScoped<MissionAPI.Interfaces.IMissionActivitySyncService, MissionAPI.Services.MissionActivitySyncService>();
builder.Services.AddScoped<MissionAPI.Interfaces.INotificationRepository, MissionAPI.Repositories.NotificationRepository>();
builder.Services.AddScoped<MissionAPI.Interfaces.INotificationService, MissionAPI.Services.NotificationService>();
builder.Services.AddScoped<MissionAPI.Interfaces.IUploadRepository, MissionAPI.Repositories.UploadRepository>();
builder.Services.AddScoped<MissionAPI.Interfaces.IUploadService, MissionAPI.Services.UploadService>();
builder.Services.AddScoped<MissionAPI.Interfaces.IWalletGrpcClient, MissionAPI.Services.WalletGrpcClient>();
builder.Services.AddScoped<MissionAPI.Interfaces.ICloudinaryService, MissionAPI.Services.CloudinaryService>();
builder.Services.Configure<MissionAPI.Settings.CloudinarySettings>(options =>
{
    options.CloudName = Environment.GetEnvironmentVariable("Cloudinary__CloudName")
        ?? Environment.GetEnvironmentVariable("CloudinarySettings__CloudName")
        ?? builder.Configuration["Cloudinary:CloudName"]
        ?? "";
    options.ApiKey = Environment.GetEnvironmentVariable("Cloudinary__ApiKey")
        ?? Environment.GetEnvironmentVariable("CloudinarySettings__ApiKey")
        ?? builder.Configuration["Cloudinary:ApiKey"]
        ?? "";
    options.ApiSecret = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret")
        ?? Environment.GetEnvironmentVariable("CloudinarySettings__ApiSecret")
        ?? builder.Configuration["Cloudinary:ApiSecret"]
        ?? "";
});

var jwtSecret = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? builder.Configuration["JwtSettings:Secret"];
if (!string.IsNullOrEmpty(jwtSecret))
{
    var key = System.Text.Encoding.ASCII.GetBytes(jwtSecret);
    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Dán access token JWT vào đây (không cần nhập tiền tố 'Bearer ')."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var chapterApiUrl = Environment.GetEnvironmentVariable("CHAPTER_API_URL")
    ?? builder.Configuration["GrpcEndpoints:ChapterAPI"]
    ?? "https://localhost:7114";
builder.Services.AddGrpcClient<ChapterGrpc.ChapterGrpcClient>(o => o.Address = new Uri(chapterApiUrl));

var socialApiUrl = Environment.GetEnvironmentVariable("SOCIAL_API_URL")
    ?? builder.Configuration["GrpcEndpoints:SocialAPI"]
    ?? "https://localhost:7133";
builder.Services.AddGrpcClient<SocialActivity.SocialActivityClient>(o => o.Address = new Uri(socialApiUrl));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<MissionDbContext>();
    db?.Database.Migrate();
    if (db != null && !db.Missions.Any())
    {
        db.Missions.AddRange(
            new MissionAPI.Entities.Mission { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Title = "Đọc Truyện Chăm Chỉ", Description = "Đọc ít nhất 5 chương truyện bất kỳ.", RewardCoin = 50, Type = SharedKernel.Enums.MissionType.ReadChapter, TargetCount = 5, IsActive = true, StartDate = new DateTime(2026, 1, 1), CreatedAt = new DateTime(2026, 1, 1) },
            new MissionAPI.Entities.Mission { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Title = "Mua Chapter", Description = "Mua ít nhất một chapter.", RewardCoin = 200, Type = SharedKernel.Enums.MissionType.PurchaseChapter, TargetCount = 1, IsActive = true, StartDate = new DateTime(2026, 1, 1), CreatedAt = new DateTime(2026, 1, 1) },
            new MissionAPI.Entities.Mission { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Title = "Nhà Phê Bình", Description = "Bình luận ít nhất 3 lần.", RewardCoin = 30, Type = SharedKernel.Enums.MissionType.LeaveComment, TargetCount = 3, IsActive = true, StartDate = new DateTime(2026, 1, 1), CreatedAt = new DateTime(2026, 1, 1) });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS is terminated at ApiGateway; internal service traffic stays on HTTP.

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<MissionAPI.GrpcServices.MissionProgressGrpcService>();
app.MapControllers();

app.Run();
