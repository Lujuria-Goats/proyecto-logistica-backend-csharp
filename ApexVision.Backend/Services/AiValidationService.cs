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
            // Solicitamos Tags, Descripción y Objetos para tener más contexto
            var features = new List<VisualFeatureTypes?> { 
                VisualFeatureTypes.Tags, 
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Objects
            };

            try 
            {
                var result = await _client.AnalyzeImageAsync(imageUrl, features);

                // 1. ANÁLISIS DE TAGS (Etiquetas)
                var defaultTags = new[] { 
                    "box", "package", "parcel", "delivery", "shipping", "cardboard", "carton", "container", 
                    "caja", "paquete", "envio", "bulto", "regalo", "bolsa", "bag", "sack", "luggage", "suitcase",
                    "envelope", "mail", "post", "label", "sticker", "plastic", "wrapping", "polybag",
                    // Contexto de entrega (Ubicación y Receptor)
                    "door", "doorway", "floor", "ground", "porch", "entrance", "puerta", "suelo", "piso", "entrada",
                    "hand", "holding", "person", "mano", "sosteniendo", "barcode", "qr code"
                };

                var tagsToValidate = (_validTags != null && _validTags.Length > 0) ? _validTags : defaultTags;
                
                // Ajustamos la confianza al 70% (muy estricto)
                bool hasValidTag = result.Tags.Any(tag => 
                    tagsToValidate.Contains(tag.Name, StringComparer.OrdinalIgnoreCase) && tag.Confidence > 0.70);

                if (hasValidTag) return true;

                // 2. ANÁLISIS DE DESCRIPCIÓN (Frases)
                // A veces no hay tag "box" pero la descripción dice "a brown square object on the floor"
                if (result.Description != null && result.Description.Captions != null)
                {
                    var validPhrases = new[] { "box", "package", "bag", "luggage", "carton", "caja", "paquete", "bolsa" };
                    bool hasValidCaption = result.Description.Captions.Any(c => 
                        validPhrases.Any(phrase => c.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase)) && c.Confidence > 0.70);
                    
                    if (hasValidCaption) return true;
                }

                // 3. ANÁLISIS DE OBJETOS (Object Detection)
                // Detecta objetos físicos específicos
                if (result.Objects != null)
                {
                    bool hasValidObject = result.Objects.Any(o => 
                        tagsToValidate.Contains(o.ObjectProperty, StringComparer.OrdinalIgnoreCase));
                    
                    if (hasValidObject) return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Si falla Azure (ej. timeout), aprobamos la imagen para no bloquear al conductor (Fail Safe)
                // En producción real podrías querer loguear esto.
                return true; 
            }
        }
    }
}

