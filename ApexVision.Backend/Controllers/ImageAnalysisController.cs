using ApexVision.Backend.DTOs;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexVision.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Solo administradores pueden acceder a estas funciones de IA
    public class ImageAnalysisController : ControllerBase
    {
        private readonly IImageAnalysisService _imageAnalysisService;
        private readonly ILogger<ImageAnalysisController> _logger;

        public ImageAnalysisController(IImageAnalysisService imageAnalysisService, ILogger<ImageAnalysisController> logger)
        {
            _imageAnalysisService = imageAnalysisService;
            _logger = logger;
        }

        /// <summary>
        /// Analiza una imagen usando Azure AI Vision
        /// Detecta objetos, texto, y genera descripción
        /// </summary>
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage([FromBody] AnalyzeImageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest(new { message = "Se requiere la URL de la imagen." });
            }

            try
            {
                var analysis = await _imageAnalysisService.AnalyzeImageAsync(request.ImageUrl);
                
                var response = new ImageAnalysisResponseDto
                {
                    Description = analysis.Description,
                    Tags = analysis.Tags,
                    Objects = analysis.Objects
                        .Select(o => new DetectedObjectDto 
                        { 
                            Name = o.Name, 
                            Confidence = o.Confidence 
                        })
                        .ToList(),
                    ExtractedText = analysis.ExtractedText,
                    Confidence = analysis.Confidence
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar imagen: {ImageUrl}", request.ImageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error al analizar la imagen.", details = ex.Message });
            }
        }

        /// <summary>
        /// Valida si una imagen es una evidencia válida
        /// Verifica calidad, claridad y contenido
        /// </summary>
        [HttpPost("validate-evidence")]
        public async Task<IActionResult> ValidateEvidencePhoto([FromBody] ValidateEvidenceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest(new { message = "Se requiere la URL de la imagen." });
            }

            try
            {
                var analysis = await _imageAnalysisService.AnalyzeImageAsync(request.ImageUrl);
                var isValid = await _imageAnalysisService.ValidateEvidencePhotoAsync(request.ImageUrl);

                var response = new EvidenceValidationResponseDto
                {
                    IsValid = isValid,
                    Confidence = analysis.Confidence,
                    ValidationMessages = analysis.ValidationMessages,
                    DetectedObjects = analysis.Objects
                        .Select(o => o.Name)
                        .ToList(),
                    Description = analysis.Description
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar evidencia: {ImageUrl}", request.ImageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error al validar la imagen de evidencia.", details = ex.Message });
            }
        }

        /// <summary>
        /// Extrae texto de una imagen (OCR)
        /// </summary>
        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractText([FromBody] ExtractTextRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest(new { message = "Se requiere la URL de la imagen." });
            }

            try
            {
                var extractedText = await _imageAnalysisService.ExtractTextAsync(request.ImageUrl);

                return Ok(new 
                { 
                    success = true,
                    extractedText,
                    characterCount = extractedText.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer texto: {ImageUrl}", request.ImageUrl);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error al extraer texto de la imagen.", details = ex.Message });
            }
        }
    }
}
