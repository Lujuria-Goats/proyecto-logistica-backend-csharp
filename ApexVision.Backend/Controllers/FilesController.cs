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

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public FilesController(IPhotoService photoService, ILogger<FilesController> logger)
        {
            _photoService = photoService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
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
    }
}
