using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexVision.Backend.Services
{
    /// <summary>
    /// Servicio para analizar imágenes usando Azure AI Vision
    /// Detecta objetos, texto, calidad, etc. en las fotos de evidencia
    /// </summary>
    public interface IImageAnalysisService
    {
        /// <summary>
        /// Analiza una imagen y devuelve información sobre su contenido
        /// </summary>
        Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl);

        /// <summary>
        /// Valida si la imagen de evidencia cumple con los requisitos
        /// </summary>
        Task<bool> ValidateEvidencePhotoAsync(string imageUrl);

        /// <summary>
        /// Extrae texto de la imagen (OCR)
        /// </summary>
        Task<string> ExtractTextAsync(string imageUrl);
    }

    public class ImageAnalysisResult
    {
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<DetectedObject> Objects { get; set; } = new();
        public string ExtractedText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public bool IsValidEvidence { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
    }

    public class DetectedObject
    {
        public string Name { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    public class AzureImageAnalysisService : IImageAnalysisService
    {
        private readonly ILogger<AzureImageAnalysisService> _logger;
        private readonly ImageAnalysisClient _client;
        private readonly bool _isConfigured;

        public AzureImageAnalysisService(IConfiguration configuration, ILogger<AzureImageAnalysisService> logger)
        {
            _logger = logger;
            var endpoint = configuration["Azure:Vision:Endpoint"];
            var key = configuration["Azure:Vision:Key"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key) || endpoint.Contains("your-region"))
            {
                _logger.LogWarning("Azure Vision credentials not configured. Image analysis is disabled.");
                _isConfigured = false;
                _client = new ImageAnalysisClient(new Uri("https://dummy.cognitiveservices.azure.com/"), new AzureKeyCredential("dummy"));
                return;
            }
            
            _isConfigured = true;
            _client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        private void CheckConfiguration()
        {
            if (!_isConfigured)
            {
                _logger.LogError("Azure Vision Service is not configured. Cannot perform analysis.");
                throw new InvalidOperationException("Azure Vision Service is not configured. Please check your settings.");
            }
        }

        /// <summary>
        /// Analiza una imagen y devuelve información detallada
        /// </summary>
        public async Task<ImageAnalysisResult> AnalyzeImageAsync(string imageUrl)
        {
            CheckConfiguration();

            try
            {
                _logger.LogInformation("Analyzing image: {ImageUrl}", imageUrl);

                var features =
                    VisualFeatures.Caption |
                    VisualFeatures.Tags |
                    VisualFeatures.Objects |
                    VisualFeatures.Read; // Use Read for OCR

                var result = await _client.AnalyzeAsync(new Uri(imageUrl), features);

                var analysisResult = new ImageAnalysisResult();

                if (result.Value.Caption != null)
                {
                    analysisResult.Description = result.Value.Caption.Text ?? "No se detectó descripción";
                    analysisResult.Confidence = (float)result.Value.Caption.Confidence;
                }

                if (result.Value.Tags != null)
                {
                    analysisResult.Tags = result.Value.Tags.Values.Select(t => t.Name).ToList();
                }

                if (result.Value.Objects != null)
                {
                    analysisResult.Objects = result.Value.Objects.Values.Select(o => new DetectedObject
                    {
                        Name = o.Tags.FirstOrDefault()?.Name ?? "Unknown",
                        Confidence = (float)(o.Tags.FirstOrDefault()?.Confidence ?? 0.0)
                    }).ToList();
                }

                if (result.Value.Read != null)
                {
                    analysisResult.ExtractedText = string.Join(" ", result.Value.Read.Blocks.SelectMany(b => b.Lines).Select(l => l.Text));
                }

                _logger.LogInformation("Analysis completed. Tags: {Tags}", string.Join(", ", analysisResult.Tags));
                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar imagen: {ImageUrl}", imageUrl);
                throw;
            }
        }

        /// <summary>
        /// Valida si la imagen es una evidencia válida
        /// Verifica que la imagen tenga suficiente calidad y claridad
        /// </summary>
        public async Task<bool> ValidateEvidencePhotoAsync(string imageUrl)
        {
            try
            {
                var analysis = await AnalyzeImageAsync(imageUrl);
                var validationMessages = new List<string>();

                if (analysis.Confidence < 0.5f)
                {
                    validationMessages.Add("La imagen es poco clara o de baja calidad.");
                }

                if (!analysis.Objects.Any())
                {
                    validationMessages.Add("No se detectaron objetos en la imagen.");
                }

                if (analysis.Tags.Contains("text") && string.IsNullOrEmpty(analysis.ExtractedText))
                {
                    validationMessages.Add("La imagen contiene texto pero no se pudo extraer.");
                }

                analysis.IsValidEvidence = !validationMessages.Any();
                analysis.ValidationMessages = validationMessages.Any() ? validationMessages : new List<string> { "✅ Imagen válida como evidencia" };

                _logger.LogInformation("Resultado de la validación de evidencia: IsValid={IsValid}, Messages={Messages}", analysis.IsValidEvidence, string.Join(", ", analysis.ValidationMessages));
                return analysis.IsValidEvidence;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la validación de evidencia para la imagen: {ImageUrl}", imageUrl);
                return false;
            }
        }

        /// <summary>
        /// Extrae todo el texto visible en la imagen (OCR)
        /// Útil para extraer datos de documentos en la foto de evidencia
        /// </summary>
        public async Task<string> ExtractTextAsync(string imageUrl)
        {
            CheckConfiguration();
            
            try
            {
                _logger.LogInformation("Extracting text from image: {ImageUrl}", imageUrl);

                var result = await _client.AnalyzeAsync(new Uri(imageUrl), VisualFeatures.Read); // Use Read for OCR

                var extractedText = string.Join("\n", result.Value.Read?.Blocks.SelectMany(b => b.Lines).Select(l => l.Text) ?? Enumerable.Empty<string>());

                _logger.LogInformation("Extracted {CharacterCount} characters of text.", extractedText.Length);
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer texto de la imagen: {ImageUrl}", imageUrl);
                throw;
            }
        }
    }
}
