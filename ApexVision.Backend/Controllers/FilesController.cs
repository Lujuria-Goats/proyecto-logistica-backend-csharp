using System.IO;
using System.Linq;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexVision.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly ILogger<FilesController> _logger;
        private readonly IImageAnalysisService _imageAnalysisService;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public FilesController(IPhotoService photoService, ILogger<FilesController> logger, IImageAnalysisService imageAnalysisService)
        {
            _photoService = photoService;
            _logger = logger;
            _imageAnalysisService = imageAnalysisService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new FileUploadResponse
                {
                    Success = false,
                    Message = "No se ha proporcionado ningún archivo."
                });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return BadRequest(new FileUploadResponse
                {
                    Success = false,
                    Message = "Formato de archivo no permitido."
                });
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(new FileUploadResponse
                {
                    Success = false,
                    Message = "El archivo es demasiado grande. El tamaño máximo permitido es 5 MB."
                });
            }

            try
            {
                var uploadResult = await _photoService.AddPhotoAsync(file);

                return Ok(new FileUploadResponse
                {
                    Success = true,
                    Url = uploadResult.SecureUrl?.ToString(),
                    PublicId = uploadResult.PublicId,
                    Message = "Archivo subido exitosamente."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir el archivo a Cloudinary");
                return StatusCode(StatusCodes.Status500InternalServerError, new FileUploadResponse
                {
                    Success = false,
                    Message = "Error interno del servidor al subir el archivo."
                });
            }
        }

        [HttpDelete("{publicId}")]
        public async Task<IActionResult> Delete(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return BadRequest(new { message = "Se requiere el identificador del recurso." });
            }

            try
            {
                await _photoService.DeletePhotoAsync(publicId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el recurso de Cloudinary");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor al eliminar el recurso." });
            }
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
                    extractedText = extractedText,
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

