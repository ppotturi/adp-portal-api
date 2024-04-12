using ADP.Portal.Core.Ado.Entities;
using Mapster;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace ADP.Portal.Api.Mapster
{
    public static class AdoProjectMapping
    {
        public static void Configure()
        {
            TypeAdapterConfig<AdoVariableGroup, VariableGroupParameters>.NewConfig()
                .Map(dest => dest.VariableGroupProjectReferences, src => new List<VariableGroupProjectReference>() { new() { Name = src.Name, Description = src.Description } })
                .Map(dest => dest.Variables, src => src.Variables.ToDictionary(v => v.Name, v => new VariableValue(v.Value, v.IsSecret)));
        }
    }
}
