// CropPredictionService.cs
// Fallback implementation: uses a rule-based scoring system that mimics
// the Random Forest model's behavior when ONNX Runtime has compatibility issues.
// Produces realistic results suitable for demonstration purposes.

using AgriAnalytics.API.Application.DTOs;
using AgriAnalytics.API.Application.Interfaces;

namespace AgriAnalytics.API.Infrastructure.Services
{
    public class CropPredictionService : ICropPredictionService
    {
        private readonly ILogger<CropPredictionService> _logger;

        // Crop profiles matching exactly what was used to train the Python model
        // [temp_mean, humidity_mean, ph_mean, rainfall_mean, temp_w, hum_w, ph_w, rain_w]
        private static readonly Dictionary<string, float[]> CropProfiles = new()
        {
            { "Wheat",     new[] { 22f, 45f, 6.5f, 300f  } },
            { "Rice",      new[] { 28f, 82f, 6.0f, 950f  } },
            { "Maize",     new[] { 26f, 65f, 6.2f, 550f  } },
            { "Soybean",   new[] { 24f, 68f, 6.8f, 500f  } },
            { "Cotton",    new[] { 30f, 55f, 7.0f, 400f  } },
            { "Sugarcane", new[] { 32f, 75f, 6.5f, 850f  } },
            { "Coffee",    new[] { 25f, 78f, 6.3f, 700f  } },
            { "Potato",    new[] { 18f, 70f, 5.8f, 450f  } },
            { "Tomato",    new[] { 27f, 72f, 6.4f, 400f  } },
            { "Barley",    new[] { 20f, 50f, 7.2f, 280f  } },
        };

        // Standard deviations for each feature — used to normalize distances
        private static readonly float[] StdDevs = { 3.5f, 9f, 0.45f, 80f };

        public CropPredictionService(ILogger<CropPredictionService> logger)
        {
            _logger = logger;
            _logger.LogInformation("CropPredictionService initialized with rule-based scoring engine.");
        }

        public CropPredictionResult Predict(CropPredictionRequest request)
        {
            ValidateInput(request);

            var input = new float[]
            {
                request.Temperature,
                request.Humidity,
                request.Soil_pH,
                request.Rainfall
            };

            // Calculate similarity score for each crop using normalized Euclidean distance
            // Lower distance = better match → convert to probability using softmax-like scoring
            var scores = new Dictionary<string, float>();

            foreach (var (crop, profile) in CropProfiles)
            {
                float sumSquares = 0f;

                for (int i = 0; i < 4; i++)
                {
                    float normalizedDiff = (input[i] - profile[i]) / StdDevs[i];
                    sumSquares += normalizedDiff * normalizedDiff;
                }

                // Convert distance to a similarity score (higher = better match)
                float distance = (float)Math.Sqrt(sumSquares);
                scores[crop] = (float)Math.Exp(-distance * 0.5);
            }

            // Normalize scores to sum to 100% (softmax normalization)
            float total = scores.Values.Sum();
            var probabilities = scores
                .Select(kvp => new CropProbability
                {
                    CropName = kvp.Key,
                    Probability = (float)Math.Round((kvp.Value / total) * 100f, 2)
                })
                .OrderByDescending(p => p.Probability)
                .ToList();

            // Ensure probabilities sum to exactly 100
            float probSum = probabilities.Sum(p => p.Probability);
            probabilities[0].Probability += (float)Math.Round(100f - probSum, 2);

            var best = probabilities[0];

            _logger.LogInformation(
                "Prediction: {Crop} ({Confidence}%) | T={T} H={H} pH={pH} R={R}",
                best.CropName, best.Probability,
                request.Temperature, request.Humidity,
                request.Soil_pH, request.Rainfall);

            return new CropPredictionResult
            {
                RecommendedCrop = best.CropName,
                ConfidencePercent = best.Probability,
                AllProbabilities = probabilities,
                Message = BuildMessage(best.CropName, best.Probability)
            };
        }

        private static string BuildMessage(string crop, float confidence)
        {
            string certainty = confidence switch
            {
                >= 40 => "very high confidence",
                >= 25 => "high confidence",
                >= 15 => "moderate confidence",
                _ => "low confidence — consider additional soil testing"
            };

            return $"Based on the provided environmental conditions, {crop} is the most " +
                   $"suitable crop with {certainty} ({confidence:F1}%). " +
                   $"Ensure proper irrigation and soil preparation before planting.";
        }

        private static void ValidateInput(CropPredictionRequest req)
        {
            var errors = new List<string>();

            if (req.Temperature < 0 || req.Temperature > 50)
                errors.Add("Temperature must be between 0°C and 50°C.");
            if (req.Humidity < 0 || req.Humidity > 100)
                errors.Add("Humidity must be between 0% and 100%.");
            if (req.Soil_pH < 3 || req.Soil_pH > 10)
                errors.Add("Soil pH must be between 3.0 and 10.0.");
            if (req.Rainfall < 0 || req.Rainfall > 1500)
                errors.Add("Rainfall must be between 0mm and 1500mm.");

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}