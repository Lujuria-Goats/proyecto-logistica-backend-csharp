using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ApexVision.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptimizerController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _javaBaseUrl;
        private readonly ILogger<OptimizerController> _logger;

        public OptimizerController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<OptimizerController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Leer desde configuración la URL base del servicio Java. Evitar doble slash al final.
            var configured = config["JavaOptimizationApi:BaseUrl"]?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(configured))
            {
                // Valor por defecto apunta al nombre del servicio en docker-compose y al puerto del contenedor Java
                configured = "http://java-backend:8080";
            }

            // Normalizar: remover slash final si existe
            _javaBaseUrl = configured.TrimEnd('/');
        }

        [HttpPost("optimize")]
        public async Task<IActionResult> Optimize()
        {
            var client = _httpClientFactory.CreateClient();
            var targetUrl = $"{_javaBaseUrl}/api/v1/optimize"; // Siempre apuntamos al path de optimización en Java

            _logger.LogInformation("Forwarding optimize request to {TargetUrl}", targetUrl);

            // Copiar el body a un MemoryStream para asegurar que el contenido se puede leer y reenviar.
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            ms.Position = 0;

            using var forwardRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StreamContent(ms)
            };

            if (!string.IsNullOrEmpty(Request.ContentType))
            {
                try
                {
                    forwardRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType);
                }
                catch
                {
                    // Si el Content-Type viene con parámetros inesperados, lo ignoramos para no romper el reenvío
                    _logger.LogWarning("Failed to set forwarded Content-Type header to '{ContentType}'", Request.ContentType);
                }
            }

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(forwardRequest);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error forwarding request to Java optimizer at {TargetUrl}", targetUrl);
                return StatusCode(500, new { message = "Internal server error.", details = ex.Message });
            }

            var content = await resp.Content.ReadAsStringAsync();

            _logger.LogInformation("Received {StatusCode} from Java optimizer", resp.StatusCode);

            return new ContentResult
            {
                Content = content,
                ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)resp.StatusCode
            };
        }
    }
}
