// MonthlyTrendDto.cs
// DTO representing aggregated monthly averages of agricultural metrics.
// Used by the analytics endpoint to feed data into the frontend charts.

namespace AgriAnalytics.API.Application.DTOs
{
    public class MonthlyTrendDto
    {
        // Label for the X-axis on charts (e.g., "Jan 2024", "Feb 2024")
        public string MonthLabel { get; set; } = string.Empty;

        // Numeric year — useful for filtering or sorting on the frontend
        public int Year { get; set; }

        // Month number (1–12) — useful for sorting before sending to client
        public int Month { get; set; }

        // Average temperature across all records in this month
        public float AvgTemperature { get; set; }

        // Average humidity across all records in this month
        public float AvgHumidity { get; set; }

        // Average soil pH across all records in this month
        public float AvgSoilPH { get; set; }

        // Average rainfall across all records in this month
        public float AvgRainfall { get; set; }

        // Total number of data points that contributed to this month's averages
        public int RecordCount { get; set; }
    }
}