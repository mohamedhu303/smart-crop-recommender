// DashboardSummaryDto.cs
// Summary statistics shown in the KPI cards at the top of the dashboard.
// Gives the viewer an instant snapshot of the dataset scale and quality.

namespace AgriAnalytics.API.Application.DTOs
{
    public class DashboardSummaryDto
    {
        // Total number of agricultural records stored in the database
        public int TotalRecords { get; set; }

        // The earliest recorded date in the dataset
        public DateTime EarliestRecord { get; set; }

        // The most recent recorded date in the dataset
        public DateTime LatestRecord { get; set; }

        // Overall average temperature across all records
        public float OverallAvgTemperature { get; set; }

        // Overall average humidity across all records
        public float OverallAvgHumidity { get; set; }

        // Overall average rainfall across all records
        public float OverallAvgRainfall { get; set; }

        // Number of distinct months covered by the dataset
        public int MonthsCovered { get; set; }
    }
}