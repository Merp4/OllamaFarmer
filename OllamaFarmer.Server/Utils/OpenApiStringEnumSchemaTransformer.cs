using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OllamaFarmer.Server.Utils
{
    public class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonTypeInfo.Type.IsEnum)
            {
                schema.Type = "string";
                schema.Enum.Clear();
                
                var enumNames = Enum.GetNames(context.JsonTypeInfo.Type);
                foreach (var name in enumNames)
                {
                    // Convert to camelCase to match your JsonNamingPolicy
                    var camelCaseName = char.ToLowerInvariant(name[0]) + name.Substring(1);
                    schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(camelCaseName));
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
