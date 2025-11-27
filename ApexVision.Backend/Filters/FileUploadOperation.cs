using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace ApexVision.Backend.Filters
{
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileUploadMime = "multipart/form-data";
            if (operation.RequestBody == null || 
                !operation.RequestBody.Content.Any(x => x.Key.Equals(fileUploadMime, StringComparison.InvariantCultureIgnoreCase)))
                return;

            var fileParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile))
                .Select(p => p.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            if (!fileParams.Any())
                return;

            operation.RequestBody.Content[fileUploadMime].Schema.Properties =
                fileParams.ToDictionary(
                    name => name,
                    _ => new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    });
        }
    }
}
