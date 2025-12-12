using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

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

            // Enable buffering so we can read the request body and then rewind it to forward it
            Request.EnableBuffering();

            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Reset original request body position in case other middleware needs it
            Request.Body.Position = 0;

            using var forwardRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StreamContent(memoryStream)
            };

            if (!string.IsNullOrEmpty(Request.ContentType))
                forwardRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType);

            // Forward headers from the original request, excluding Host and Content-Length
            foreach (var header in Request.Headers.Where(h => !string.Equals(h.Key, "Host", System.StringComparison.OrdinalIgnoreCase)
                                                               && !string.Equals(h.Key, "Content-Length", System.StringComparison.OrdinalIgnoreCase)))
            {
                // Try add to request headers first, otherwise to content headers
                if (!forwardRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    forwardRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            var resp = await client.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead);
            var content = await resp.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)resp.StatusCode
            };
        }
    }
}
