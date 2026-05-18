// AgriDataRecord.cs
// Core domain entity representing a single agricultural/weather observation record.
// This is a pure domain object — no EF attributes, no framework dependencies here.

namespace AgriAnalytics.API.Domain.Entities
{
    public class AgriDataRecord
    {
        public int Id { get; set; }

        // Ambient temperature in Celsius at the time of recording
        public float Temperature { get; set; }

        // Relative humidity percentage (0–100)
        public float Humidity { get; set; }

        // Soil acidity/alkalinity on the pH scale (4.0–9.0)
        public float Soil_pH { get; set; }

        // Total rainfall in millimeters for the recorded period
        public float Rainfall { get; set; }

        // The actual crop grown under these conditions (optional, used for labeling)
        public string? CropLabel { get; set; }

        // Timestamp of when this observation was recorded
        public DateTime DateRecorded { get; set; }
    }
}