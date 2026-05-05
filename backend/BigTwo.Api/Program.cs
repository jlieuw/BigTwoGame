using BigTwo.Api.Hubs;
using BigTwo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<GameLogicService>();
builder.Services.AddSingleton<RoomService>();

// Origins can be overridden at deploy time via the AllowedOrigins config key.
// In production the nginx proxy makes CORS moot (requests arrive from an internal IP),
// but the explicit list is kept as a safety net for direct-access scenarios.
var allowedOrigins = (builder.Configuration["AllowedOrigins"]
    ?? "http://localhost:3000,http://localhost:5173,http://localhost:80,http://frontend")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapHub<GameHub>("/gamehub");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
