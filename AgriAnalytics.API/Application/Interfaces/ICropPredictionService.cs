// ICropPredictionService.cs
// Contract for the ML inference service.
// The application layer depends on this abstraction, not on the concrete
// ONNX implementation — keeping the domain and application layers
// completely free of infrastructure concerns.

using AgriAnalytics.API.Application.DTOs;

namespace AgriAnalytics.API.Application.Interfaces
{
    public interface ICropPredictionService
    {
        // Runs the ONNX model inference and returns a ranked prediction result.
        // Throws InvalidOperationException if the model file is not found or input is invalid.
        CropPredictionResult Predict(CropPredictionRequest request);
    }
}