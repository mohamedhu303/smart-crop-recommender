// CropPredictionRequest.cs
// Data Transfer Object that carries the user's input from the API endpoint
// into the application layer for processing.

namespace AgriAnalytics.API.Application.DTOs
{
    public class CropPredictionRequest
    {
        // Temperature in Celsius — expected range: 5 to 45
        public float Temperature { get; set; }

        // Humidity percentage — expected range: 20 to 100
        public float Humidity { get; set; }

        // Soil pH level — expected range: 4.0 to 9.0
        public float Soil_pH { get; set; }

        // Rainfall in millimeters — expected range: 50 to 1200
        public float Rainfall { get; set; }
    }
}