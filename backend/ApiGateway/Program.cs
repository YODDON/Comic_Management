using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SharedKernel.Responses;
using System.Text.Json;
using DotNetEnv;
using Microsoft.AspNetCore.Authorization;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var secret = Environment.GetEnvironmentVariable("JwtSettings__Secret")
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

var frontendOrigin = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode is 401 or 404 or 502 or 503)
    {
        if (!context.Response.HasStarted)
        {
            if (context.Response.StatusCode == 502)
            {
                context.Response.StatusCode = 503;
            }

            context.Response.ContentType = "application/json";
            context.Response.ContentLength = null;
            var message = context.Response.StatusCode switch
            {
                401 => "Unauthorized access",
                404 => "Resource not found",
                503 => "Service unavailable",
                _ => "An error occurred"
            };
            
            var apiResponse = ApiResponse<object>.ErrorResponse(message, context.Response.StatusCode);
            var json = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    // Let unmatched paths reach the normal 404 handler instead of turning them into 401.
    if (context.GetEndpoint() is null)
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value?.ToLower() ?? "";

    var isGet = HttpMethods.IsGet(context.Request.Method);
    var isPost = HttpMethods.IsPost(context.Request.Method);
    bool isPublic =
        path == "/auth/login" ||
        path == "/auth/register" ||
        path == "/auth/forgot-password" ||
        path == "/auth/reset-password" ||
        path == "/auth/refetchtoken" ||
        path == "/auth/resend-confirm" ||
        path == "/auth/google" ||
        path.StartsWith("/auth/verify") ||
        (path.StartsWith("/banners") && isGet) ||
        (path.StartsWith("/categories") && isGet) ||
        (path.StartsWith("/translations") && isPost) ||
        (path.StartsWith("/comics") && isGet && !path.StartsWith("/comics/me") && !path.StartsWith("/comics/purchased")) ||
        (path.StartsWith("/chapters") && isGet) ||
        (path.StartsWith("/comments") && isGet) ||
        (path == "/payments/sepay-webhook" && isPost);

    if (!isPublic && (context.User.Identity == null || !context.User.Identity.IsAuthenticated))
    {
        context.Response.StatusCode = 401;
        return; // Will be caught by the middleware above to return standard response
    }

    await next();
});

app.MapReverseProxy();

app.Run();
