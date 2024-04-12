using Mapster;
using System.Reflection;

namespace ADP.Portal.Api.Mapster
{
    public static class MapsterConfig
    {
        public static void Configure(this IServiceCollection services)
        {
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

            AdoProjectMapping.Configure();

            AadGroupMapping.Configure();

            FluxTeamConfigMappings.Configure();
        }
    }
}
