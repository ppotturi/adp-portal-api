using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Entities
{
    public class FluxService
    {
        public required string Name { get; set; }
        public required FluxServiceType Type { get; set; }
        public required List<FluxEnvironment> Environments { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<FluxConfig> ConfigVariables { get; set; } = [];
    }

    public enum FluxServiceType
    {
        Frontend,
        Backend,
        HelmOnly
    }

    public static class FluxServiceExtensions
    {
        public static bool HasDatastore(this FluxService service)
        {
            return service.ConfigVariables.Exists(token => token.Key.Equals(FluxConstants.POSTGRES_DB_KEY));
        }
    }
}