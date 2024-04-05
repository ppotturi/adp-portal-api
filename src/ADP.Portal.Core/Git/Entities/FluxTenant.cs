using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Entities
{
    public class FluxTenant
    {
        public required List<FluxEnvironment> Environments { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<FluxConfig> ConfigVariables { get; set; } = [];
    }
}
