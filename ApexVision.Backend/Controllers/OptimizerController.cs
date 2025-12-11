using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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

            using var forwardRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StreamContent(Request.Body)
            };

            if (!string.IsNullOrEmpty(Request.ContentType))
                forwardRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType);

            var resp = await client.SendAsync(forwardRequest);
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

