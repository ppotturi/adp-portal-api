using YamlDotNet.Serialization;

namespace ADP.Portal.Core.Git.Entities
{
    public class FluxTeamConfig
    {
        public required string ServiceCode { get; set; }
        public required string ProgrammeName { get; set; }
        public required string TeamName { get; set; }
        public required List<FluxService> Services { get; set; }

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults)]
        public List<FluxConfig> ConfigVariables { get; set; } = [];
    }
}
