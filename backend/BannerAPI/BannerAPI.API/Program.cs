using Microsoft.EntityFrameworkCore;
using BannerAPI.Data;
using BannerAPI.Interfaces;
using BannerAPI.Repositories;
using BannerAPI.Services;
using SharedKernel.Extensions;

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

builder.Services.AddCustomJwtAuthentication(builder.Configuration);

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
