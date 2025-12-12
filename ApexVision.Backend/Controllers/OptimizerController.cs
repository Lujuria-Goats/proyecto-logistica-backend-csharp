using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ApexVision.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptimizerController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _javaBaseUrl;

        public OptimizerController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _javaBaseUrl = config["JavaOptimizationApi:BaseUrl"]?.TrimEnd('/') ?? "http://apex_java:8080";
        }

        [HttpPost("optimize")]
        public async Task<IActionResult> Optimize()
        {
            var client = _httpClientFactory.CreateClient();
            var targetUrl = $"{_javaBaseUrl}/api/v1/optimize";

            // Leer el body como string
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Crear el contenido para reenviar
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(targetUrl, content);
            var respContent = await resp.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = respContent,
                ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)resp.StatusCode
            };
        }
    }
}
