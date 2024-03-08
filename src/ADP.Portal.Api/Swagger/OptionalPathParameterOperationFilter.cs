using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ADP.Portal.Api.Swagger
{
    public class OptionalPathParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                var description = context.ApiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

                if (description.RouteInfo?.IsOptional == true)
                {
                    parameter.AllowEmptyValue = true;
                    parameter.Description = "Must check \"Send empty value\" or Swagger passes a comma for empty values otherwise";
                    parameter.Required = false;
                    parameter.Schema.Nullable = true;
                    parameter.Required = false;
                }
            }
        }
    }
}
