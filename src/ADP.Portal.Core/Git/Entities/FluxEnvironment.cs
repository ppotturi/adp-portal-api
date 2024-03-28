using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Entities
{
    public class FluxEnvironment
    {
        public required string Name { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<FluxConfig> ConfigVariables { get; set; } = [];
    }
}
