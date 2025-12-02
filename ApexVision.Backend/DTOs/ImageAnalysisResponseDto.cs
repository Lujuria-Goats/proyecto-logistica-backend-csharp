namespace ApexVision.Backend.DTOs
{
    /// <summary>
    /// Respuesta del análisis de imagen con Azure AI Vision
    /// </summary>
    public class ImageAnalysisResponseDto
    {
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<DetectedObjectDto> Objects { get; set; } = new();
        public string ExtractedText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public bool IsValidEvidence { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
    }

    /// <summary>
    /// Objeto detectado en la imagen
    /// </summary>
    public class DetectedObjectDto
    {
        public string Name { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}

