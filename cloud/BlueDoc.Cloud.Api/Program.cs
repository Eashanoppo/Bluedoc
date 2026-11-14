using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BlueDoc.Cloud.Api.Data;
using BlueDoc.Cloud.Api.Middleware;
using BlueDoc.Cloud.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Database=bluedoc;Username=bluedoc;Password=bluedoc_secret";

builder.Services.AddDbContext<BluedocCloudDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register Domain Services
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<TelemetryIngestionService>();
builder.Services.AddScoped<AlertEvaluationService>();
builder.Services.AddScoped<FleetAggregationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlueDoc Cloud Fleet API",
        Version = "v1",
        Description = "Enterprise Central Fleet Telemetry & Management REST API"
    });
});

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlueDoc Cloud Fleet API v1");
    c.RoutePrefix = "swagger";
});

// Custom API Key Authentication Middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

// Auto-create database tables if not existing (optional convenience for standalone startup)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BluedocCloudDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"[DB Notice] Database EnsureCreated deferred: {ex.Message}");
    }
}

app.Run();

