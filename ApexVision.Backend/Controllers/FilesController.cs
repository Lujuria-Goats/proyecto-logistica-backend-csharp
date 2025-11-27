using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        // Usamos la Interfaz, no la clase concreta (Buena práctica)
        private readonly IPhotoService _photoService;

        public FilesController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // CORRECCIÓN AQUÍ: Usamos AddPhotoAsync
            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) 
                return BadRequest(result.Error.Message);

            return Ok(new { url = result.SecureUrl.ToString(), publicId = result.PublicId });
        }

        [HttpDelete("{publicId}")]
        public async Task<IActionResult> Delete(string publicId)
        {
            // CORRECCIÓN AQUÍ: Usamos DeletePhotoAsync
            var result = await _photoService.DeletePhotoAsync(publicId);

            if (result.Result == "ok") 
                return Ok();

            return BadRequest("No se pudo eliminar la imagen");
        }
    }
}