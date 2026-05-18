// IAnalyticsService.cs
// Contract for the analytics data service.
// Abstracts how we retrieve and aggregate historical records from the database,
// so the endpoint handlers don't need to know anything about EF Core or SQL.

using AgriAnalytics.API.Application.DTOs;

namespace AgriAnalytics.API.Application.Interfaces
{
    public interface IAnalyticsService
    {
        // Returns monthly aggregated trend data sorted chronologically.
        // Used to populate the line/bar charts on the frontend dashboard.
        Task<List<MonthlyTrendDto>> GetMonthlyTrendsAsync();

        // Returns a quick summary of total records, date range, and averages.
        // Used for the stats cards at the top of the dashboard.
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    }
}