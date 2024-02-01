using ADP.Portal.Core.Ado.Entities;
using Mapster;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Reflection;

namespace ADP.Portal.Api.Mapster
{
    public static class MapsterEntitiesConfig
    {
        public static void EntitiesConfigure(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

            TypeAdapterConfig<AdoVariableGroup, VariableGroupParameters>.NewConfig()
                .Map(dest => dest.VariableGroupProjectReferences, src => new List<VariableGroupProjectReference>() { new() { Name = src.Name, Description = src.Description } })
                .Map(dest => dest.Variables, src => src.Variables.ToDictionary(v => v.Name, v => new VariableValue(v.Value, v.IsSecret)));

        }
    }
}
