using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexVision.Backend.DTOs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Options;

namespace ApexVision.Backend.Services
{
    public class AiValidationService : IAiValidationService
    {
        private readonly ComputerVisionClient _client;

        public AiValidationService(IOptions<AzureVisionSettings> azureSettings)
        {
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(azureSettings.Value.Key))
            {
                Endpoint = azureSettings.Value.Endpoint
            };
        }

        public async Task<bool> ValidateDeliveryEvidenceAsync(string imageUrl)
        {
            var features = new List<VisualFeatureTypes?> { VisualFeatureTypes.Tags };
            var result = await _client.AnalyzeImageAsync(imageUrl, features);

            var validTags = new[] { "box", "package", "carton", "container", "door", "delivery" };

            return result.Tags.Any(tag => validTags.Contains(tag.Name, StringComparer.OrdinalIgnoreCase) && tag.Confidence > 0.7);
        }
    }
}

