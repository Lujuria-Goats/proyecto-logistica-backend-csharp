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
        private readonly string[] _validTags;

        public AiValidationService(IOptions<AzureVisionSettings> azureSettings)
        {
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(azureSettings.Value.Key))
            {
                Endpoint = azureSettings.Value.Endpoint
            };
            _validTags = azureSettings.Value.ValidTags;
        }

        public async Task<bool> ValidateDeliveryEvidenceAsync(string imageUrl)
        {
            // If no tags are configured, default to a safe list or allow everything? 
            // Requirement implies specific validation, so if config is empty, maybe fallback or fail?
            // "If not found any tag... reject". If list is empty, it will reject everything unless we have a default.
            // But I will stick to using the configured list. If empty, it's a configuration error but per logic it returns false.
            
            var features = new List<VisualFeatureTypes?> { VisualFeatureTypes.Tags };
            var result = await _client.AnalyzeImageAsync(imageUrl, features);

            // Lista de respaldo hardcoded por si la config falla o está vacía
            var defaultTags = new[] { 
                "box", "package", "parcel", "delivery", "shipping", "cardboard", "carton", "container", 
                "caja", "paquete", "envio", "bulto", "regalo", "bolsa", "bag" 
            };

            var tagsToValidate = (_validTags != null && _validTags.Length > 0) ? _validTags : defaultTags;

            // Log de los tags encontrados para depuración
            var foundTags = string.Join(", ", result.Tags.Select(t => $"{t.Name} ({t.Confidence:P0})"));
            // Console.WriteLine($"AI Analysis Tags: {foundTags}"); // Opcional para logs

            // Bajamos la confianza requerida a 0.5 (50%) para ser más permisivos
            return result.Tags.Any(tag => tagsToValidate.Contains(tag.Name, StringComparer.OrdinalIgnoreCase) && tag.Confidence > 0.5);
        }
    }
}

