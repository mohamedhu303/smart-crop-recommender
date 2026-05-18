// Program.cs
// Application entry point and composition root for AgriAnalytics API.
// Wires up all layers: EF Core, ML inference service, and API endpoints.
// Uses ASP.NET Core Minimal APIs for a clean, lightweight presentation layer.

using AgriAnalytics.API.Application.DTOs;
using AgriAnalytics.API.Application.Interfaces;
using AgriAnalytics.API.Infrastructure.Persistence;
using AgriAnalytics.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. CORS ────────────────────────────────────────────────────────────────
// Allow the Angular dev server (port 4200) to call our API during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── 2. Database ─────────────────────────────────────────────────────────────
// Register EF Core with SQL Server using the connection string from appsettings
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120) // 2 min timeout for seeding queries
    )
);

// ─── 3. Application Services ─────────────────────────────────────────────────
// Analytics service is scoped — one instance per HTTP request (uses DbContext)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Prediction service is singleton — ONNX session is expensive to create,
// so we load the model once and reuse it for the lifetime of the application
builder.Services.AddSingleton<ICropPredictionService, CropPredictionService>();

// ─── 4. Swagger ──────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AgriAnalytics API",
        Version = "v1",
        Description = "Agricultural analytics and ML-powered crop recommendation API"
    });
});

var app = builder.Build();

// ─── 5. Auto-migrate & Seed on Startup ──────────────────────────────────────
// Runs EF migrations and seeds the database automatically when the app starts.
// This means you only need to run `dotnet ef migrations add` once — never again.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");

        logger.LogInformation("Starting database seeder...");
        await DbSeeder.SeedAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during migration or seeding.");
        throw;
    }
}

// ─── 6. Middleware Pipeline ──────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgriAnalytics API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAngular");

// ─── 7. API Endpoints ────────────────────────────────────────────────────────

// Health check — quick way to verify the API is running
app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "AgriAnalytics API"
}))
.WithName("HealthCheck")
.WithTags("System");

// ── GET /api/analytics/summary ───────────────────────────────────────────────
// Returns KPI stats for the dashboard header cards:
// total records, date range, overall averages
app.MapGet("/api/analytics/summary", async (IAnalyticsService analyticsService) =>
{
    try
    {
        var summary = await analyticsService.GetDashboardSummaryAsync();
        return Results.Ok(summary);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Failed to retrieve dashboard summary",
            statusCode: 500
        );
    }
})
.WithName("GetDashboardSummary")
.WithTags("Analytics");

// ── GET /api/analytics/trends ────────────────────────────────────────────────
// Returns monthly aggregated averages for Temperature, Humidity, Soil pH, Rainfall.
// This powers the line/bar charts on the frontend — demonstrates Big Data processing.
app.MapGet("/api/analytics/trends", async (IAnalyticsService analyticsService) =>
{
    try
    {
        var trends = await analyticsService.GetMonthlyTrendsAsync();

        if (!trends.Any())
            return Results.NotFound(new { Message = "No trend data found. Database may still be seeding." });

        return Results.Ok(trends);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Failed to retrieve trend data",
            statusCode: 500
        );
    }
})
.WithName("GetMonthlyTrends")
.WithTags("Analytics");

// ── POST /api/predict-crop ───────────────────────────────────────────────────
// Accepts environmental inputs and runs ONNX model inference.
// Returns the recommended crop with confidence score and full probability breakdown.
app.MapPost("/api/predict-crop", (
    CropPredictionRequest request,
    ICropPredictionService predictionService) =>
{
    try
    {
        var result = predictionService.Predict(request);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        // Input validation errors — return 400 Bad Request with a clear message
        return Results.BadRequest(new { Message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "ML inference failed",
            statusCode: 500
        );
    }
})
.WithName("PredictCrop")
.WithTags("ML Prediction");

// ── GET /api/analytics/records ───────────────────────────────────────────────
// Returns a paginated view of raw records — useful to show the data volume in a demo
app.MapGet("/api/analytics/records", async (
    AppDbContext db,
    int page = 1,
    int pageSize = 50) =>
{
    // Cap page size to prevent accidental huge queries
    pageSize = Math.Min(pageSize, 200);

    var total = await db.AgriDataRecords.CountAsync();
    var records = await db.AgriDataRecords
        .OrderByDescending(r => r.DateRecorded)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(r => new
        {
            r.Id,
            r.Temperature,
            r.Humidity,
            r.Soil_pH,
            r.Rainfall,
            r.CropLabel,
            DateRecorded = r.DateRecorded.ToString("yyyy-MM-dd HH:mm")
        })
        .ToListAsync();

    return Results.Ok(new
    {
        TotalRecords = total,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        Data = records
    });
})
.WithName("GetRecords")
.WithTags("Analytics");

app.Run();