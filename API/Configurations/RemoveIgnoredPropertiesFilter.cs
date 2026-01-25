using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace API.Configurations
{
    /// <summary>
    /// Filtro do Swagger para remover propriedades marcadas com [JsonIgnore] dos parâmetros.
    /// 
    /// PROBLEMA:
    /// O Swagger gera parâmetros para TODAS as propriedades públicas de objetos [FromQuery],
    /// incluindo propriedades calculadas como Skip e Take.
    /// 
    /// SOLUÇÃO:
    /// Este filtro remove da documentação do Swagger qualquer parâmetro cuja propriedade
    /// tenha o atributo [JsonIgnore].
    /// 
    /// USO:
    /// Registrar no Program.cs:
    /// builder.Services.AddSwaggerGen(c => {
    ///     c.OperationFilter&lt;RemoveIgnoredPropertiesFilter&gt;();
    /// });
    /// </summary>
    public class RemoveIgnoredPropertiesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                return;

            // Lista de propriedades a remover (além de [JsonIgnore])
            var parametersToRemove = new List<OpenApiParameter>();

            foreach (var parameter in operation.Parameters)
            {
                // Procura a propriedade correspondente no tipo
                var parameterDescriptor = context.ApiDescription.ParameterDescriptions
                    .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (parameterDescriptor?.ModelMetadata?.ContainerType != null)
                {
                    var property = parameterDescriptor.ModelMetadata.ContainerType
                        .GetProperty(parameterDescriptor.ModelMetadata.PropertyName ?? parameterDescriptor.Name);

                    if (property != null)
                    {
                        // Remove se tiver [JsonIgnore]
                        var hasJsonIgnore = property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any();
                        
                        if (hasJsonIgnore)
                        {
                            parametersToRemove.Add(parameter);
                        }
                    }
                }
            }

            // Remove os parâmetros identificados
            foreach (var parameter in parametersToRemove)
            {
                operation.Parameters.Remove(parameter);
            }
        }
    }
}
