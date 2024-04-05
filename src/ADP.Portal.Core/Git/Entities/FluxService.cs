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
        Backend
    }
}