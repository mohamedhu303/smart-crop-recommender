// AnalyticsService.cs
// Retrieves and aggregates historical agricultural data from SQL Server.
// Uses EF Core LINQ queries to compute monthly averages directly in the database
// — this is intentional to demonstrate server-side "Big Data" processing.

using AgriAnalytics.API.Application.DTOs;
using AgriAnalytics.API.Application.Interfaces;
using AgriAnalytics.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriAnalytics.API.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendsAsync()
        {
            // Group all records by year and month, compute averages per group.
            // The heavy lifting happens in SQL Server — not in application memory.
            var trends = await _context.AgriDataRecords
                .GroupBy(r => new
                {
                    r.DateRecorded.Year,
                    r.DateRecorded.Month
                })
                .Select(g => new MonthlyTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthLabel = g.Key.Year + "-" + (g.Key.Month < 10 ? "0" : "") + g.Key.Month,
                    AvgTemperature = (float)Math.Round(g.Average(r => (double)r.Temperature), 2),
                    AvgHumidity = (float)Math.Round(g.Average(r => (double)r.Humidity), 2),
                    AvgSoilPH = (float)Math.Round(g.Average(r => (double)r.Soil_pH), 2),
                    AvgRainfall = (float)Math.Round(g.Average(r => (double)r.Rainfall), 2),
                    RecordCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToListAsync();

            return trends;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            // Compute overall statistics in a single database round-trip
            var summary = await _context.AgriDataRecords
                .GroupBy(_ => 1)
                .Select(g => new DashboardSummaryDto
                {
                    TotalRecords = g.Count(),
                    EarliestRecord = g.Min(r => r.DateRecorded),
                    LatestRecord = g.Max(r => r.DateRecorded),
                    OverallAvgTemperature = (float)Math.Round(g.Average(r => (double)r.Temperature), 2),
                    OverallAvgHumidity = (float)Math.Round(g.Average(r => (double)r.Humidity), 2),
                    OverallAvgRainfall = (float)Math.Round(g.Average(r => (double)r.Rainfall), 2),
                    MonthsCovered = g.Select(r => new { r.DateRecorded.Year, r.DateRecorded.Month })
                                           .Distinct()
                                           .Count()
                })
                .FirstOrDefaultAsync();

            return summary ?? new DashboardSummaryDto();
        }
    }
}