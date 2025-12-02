namespace ApexVision.Backend.DTOs
{
    /// <summary>
    /// Request para análisis de imagen
    /// </summary>
    public class AnalyzeImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para validación de evidencia
    /// </summary>
    public class ValidateEvidenceRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para extracción de texto (OCR)
    /// </summary>
    public class ExtractTextRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response para validación de evidencia
    /// </summary>
    public class EvidenceValidationResponseDto
    {
        public bool IsValid { get; set; }
        public float Confidence { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
        public List<string> DetectedObjects { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}

