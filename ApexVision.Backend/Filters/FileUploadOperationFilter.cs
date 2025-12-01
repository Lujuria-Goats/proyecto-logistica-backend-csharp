using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApexVision.Backend.Filters
{
    /// <summary>
    /// Swagger filter to properly handle IFormFile parameters in API endpoints.
    /// This resolves the Swagger generation error when using IFormFile with file uploads.
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasFormFile = context.MethodInfo.GetParameters()
                .Any(p => p.ParameterType == typeof(IFormFile) || 
                         (p.ParameterType.IsGenericType && 
                          p.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                          p.ParameterType.GenericTypeArguments[0] == typeof(IFormFile)));

            if (!hasFormFile)
                return;

            operation.RequestBody = new OpenApiRequestBody()
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "multipart/form-data",
                        new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    {
                                        "file",
                                        new OpenApiSchema
                                        {
                                            Type = "string",
                                            Format = "binary",
                                            Description = "The file to upload"
                                        }
                                    }
                                },
                                Required = new HashSet<string> { "file" }
                            }
                        }
                    }
                }
            };

            // Remove file parameter from parameters list if it exists
            var fileParams = operation.Parameters.Where(p => p.Name == "file").ToList();
            foreach (var param in fileParams)
            {
                operation.Parameters.Remove(param);
            }
        }
    }
}

