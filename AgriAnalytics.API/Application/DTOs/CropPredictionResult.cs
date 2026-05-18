// CropPredictionResult.cs
// Data Transfer Object returned to the client after running ML inference.
// Contains the predicted crop name, confidence score, and ranked alternatives.

namespace AgriAnalytics.API.Application.DTOs
{
    public class CropPredictionResult
    {
        // The top recommended crop name based on the model's prediction
        public string RecommendedCrop { get; set; } = string.Empty;

        // Confidence percentage for the top prediction (0.0 – 100.0)
        public float ConfidencePercent { get; set; }

        // Ranked list of all crops with their individual probability scores
        public List<CropProbability> AllProbabilities { get; set; } = new();

        // Human-readable message to display on the dashboard
        public string Message { get; set; } = string.Empty;
    }

    public class CropProbability
    {
        // Crop name (e.g., "Wheat", "Rice", "Maize")
        public string CropName { get; set; } = string.Empty;

        // Probability as a percentage (e.g., 87.4)
        public float Probability { get; set; }
    }
}